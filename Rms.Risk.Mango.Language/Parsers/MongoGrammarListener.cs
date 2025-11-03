/* 
 *                                dbMango
 *
 * Copyright 2025 Deutsche Bank AG
 * SPDX-License-Identifier: Apache-2.0
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using Antlr4.Runtime.Misc;
using Parser=Rms.Risk.Mango.Language.MongoAggregationForHumansParser;

namespace Rms.Risk.Mango.Language.Parsers;

public class MongoGrammarListener : MongoAggregationForHumansBaseListener
{
    private AstPipeline _currentPipeline = AstPipeline.None;
    
    private Stack<AstPipeline> _pipelines = new();
    private Stack<AstStage>    _stage     = new();

    public AstAggregation? Aggregate { get; private set;}

    private void Clear()
    {
        _currentPipeline = AstPipeline.None;
        _pipelines.Clear();
        _stage.Clear();
    }

    public override void EnterFile(Parser.FileContext context)
    {
        Clear();
        var collection = context.STRING().GetText();
        Aggregate = new(collection);
    }

    public override void ExitFile(Parser.FileContext context)
    {
        Aggregate!.Add(_currentPipeline);

        if ( ReferenceEquals( _currentPipeline, AstPipeline.None ) )
            throw new("Invalid _currentPipeline");

        if ( _pipelines.Count != 1 )
            throw new($"Invalid _pipelines Count={_pipelines.Count}");
        if ( _stage.Count != 0 )
            throw new($"Invalid _stage Count={_stage.Count}");
    }

    public override void EnterPipeline_def(MongoAggregationForHumansParser.Pipeline_defContext context)
    {
        _pipelines.Push(_currentPipeline);
        _currentPipeline = new();

        // stages are responsible for popping it back
    }

    public override void ExitStage_def(Parser.Stage_defContext context)
    {
        var stage = _currentPipeline.Stages.Last();
        if ( context.json() != null )
            stage.Options = (JsonObject)JsonListenerHelper.Convert(context.json());
    }

    public override void EnterMatch_def(Parser.Match_defContext context)
    {
        var stage = new AstStageWhere();
        _stage.Push(stage);
    }

    public override void ExitMatch_def(Parser.Match_defContext context)
    {
        var stage = _stage.Pop();
        var expression = ListenerHelper.BuildAst(context.expression());
        stage.Add(expression);
        _currentPipeline.Add(stage);
    }

    public override void EnterBucketPlain(Parser.BucketPlainContext context)
    {
        var stage = new AstStageBucket();
        _stage.Push(stage);
    }

    public override void ExitBucketPlain(Parser.BucketPlainContext context)
    {
        var stage  = (AstStageBucket)_stage.Pop();

        stage.GroupBy = ListenerHelper.BuildAst(context.expression());

        foreach (var bound in context.NUMBER().Select(x => x.GetText()).Where(x => !string.IsNullOrWhiteSpace(x)))
        {
            stage.AddBucket(bound.Trim('"'));
        }

        if (context.defaultBucket != null)
        {
            stage.DefaultBucket = context.defaultBucket.Text.Trim('"');
        }

        var fields = ListenerHelper.BuildLet(context.let_list());
        foreach (var field in fields)
            stage.Add(field);
        _currentPipeline.Add(stage);
    }

    public override void EnterBucketAuto(Parser.BucketAutoContext context)
    {
        var stage = new AstStageBucket()
        {
            Auto = true
        };
        _stage.Push(stage);
    }

    public override void ExitBucketAuto(Parser.BucketAutoContext context)
    {
        var stage  = (AstStageBucket)_stage.Pop();

        stage.GroupBy         = ListenerHelper.BuildAst(context.expression());
        stage.NumberOfBuckets = int.Parse(context.NUMBER().GetText());
        stage.Granularity     = context.STRING()?.GetText().Trim('"');

        var fields = ListenerHelper.BuildLet(context.let_list());
        foreach (var field in fields)
            stage.Add(field);

        _currentPipeline.Add(stage);
    }

    public override void EnterAddfields_def(Parser.Addfields_defContext context)
    {
        var stage = new AstStageAddFields([]);
        _stage.Push(stage);
    }

    public override void ExitAddfields_def(Parser.Addfields_defContext context)
    {
        var stage = (AstStageAddFields)_stage.Pop();
        var fields = ListenerHelper.BuildLet(context.let_list());
        foreach (var field in fields)
            stage.Add(field);
        _currentPipeline.Add(stage);
    }

    public override void EnterProjectInclude(Parser.ProjectIncludeContext context)
    {
        var stage = new AstStageProject();
        _stage.Push(stage);
    }

    public override void ExitProjectInclude(Parser.ProjectIncludeContext context)
    {
        var stage = (AstStageProject)_stage.Pop();

        var idLetList = context.id_list;
        var letList = context.data_list;

        var idFields = ListenerHelper.BuildLet(idLetList);
        foreach (var field in idFields)
            stage.AddId(field);

        var fields = ListenerHelper.BuildLet(letList);
        foreach (var field in fields)
            stage.Add(field);
        _currentPipeline.Add(stage);
    }

    public override void EnterProjectExclude(Parser.ProjectExcludeContext context)
    {
        var stage = new AstStageProject();
        _stage.Push(stage);
    }

    public override void ExitProjectExclude(Parser.ProjectExcludeContext context)
    {
        var stage = (AstStageProject)_stage.Pop();
        stage.Exclude = true;
        var fields = ListenerHelper.BuildVarList(context.var_list());
        foreach (var field in fields)
            stage.Add(field);
        _currentPipeline.Add(stage);
    }

    public override void EnterReplace_def(Parser.Replace_defContext context)
    {
        var stage = new AstStageReplace();
        _stage.Push(stage);
    }

    public override void ExitReplace_def(Parser.Replace_defContext context)
    {
        var stage = (AstStageReplace)_stage.Pop();

        var idLetList = context.id_list;
        var letList = context.data_list;

        var idFields = ListenerHelper.BuildLet(idLetList);
        foreach (var field in idFields)
            stage.AddId(field);

        var fields = ListenerHelper.BuildLet(letList);
        foreach (var field in fields)
            stage.Add(field);
        _currentPipeline.Add(stage);
    }

    public override void EnterGroup_by_def(Parser.Group_by_defContext context)
    {
        var stage = new AstStageGroupBy([], []);
        _stage.Push(stage);
    }

    public override void ExitGroup_by_def(Parser.Group_by_defContext context)
    {
        var stage = (AstStageGroupBy)_stage.Pop();
        var id = ListenerHelper.BuildLet(context.id_list);
        var fields = ListenerHelper.BuildLet(context.data_list);
        
        foreach (var field in fields)
            stage.Add(field);
        foreach (var x in id)
            stage.AddId(x);

        _currentPipeline.Add(stage);
    }

    public override void EnterSort_def(Parser.Sort_defContext context)
    {
        var stage = new AstStageSortBy();
        _stage.Push(stage);
    }

    public override void ExitSort_def(Parser.Sort_defContext context)
    {
        var stage = (AstStageSortBy)_stage.Pop();
        foreach (var field in ListenerHelper.BuildSortFieldList(context.sort_var_list()))
            stage.Add(field);

        _currentPipeline.Add(stage);
    }
    
    public override void EnterUnwind_def(Parser.Unwind_defContext context)
    {
        var stage = new AstStageUnwind();
        _stage.Push(stage);
    }

    public override void ExitUnwind_def(Parser.Unwind_defContext context)
    {
        var stage = (AstStageUnwind)_stage.Pop();
        stage.Name = context.VARIABLE()[0].GetText();

        stage.Index = context.VARIABLE().Length > 1 ? context.VARIABLE()[1].GetText() : null;


        _currentPipeline.Add(stage);
    }
    
    public override void EnterJoin_def(Parser.Join_defContext context)
    {
        var stage = new AstStageJoin();
        _stage.Push(stage);
    }

    public override void ExitJoin_def(Parser.Join_defContext context)
    {
        var stage = (AstStageJoin)_stage.Pop();

        var pipelineExists = context.pipeline_def() != null;

        AstPipeline pipeline;
        if (pipelineExists)
        {
            pipeline         = _currentPipeline;
            _currentPipeline = _pipelines.Pop();
        }
        else
            pipeline = AstPipeline.None;

            var on = ListenerHelper.BuildEquivalence(context.equivalence_list());
        var collection = context.STRING()[0].GetText();
        
        var asField =
               context.VARIABLE()?.GetText()
            ?? context.STRING()[1].GetText()
            ?? throw new ($"AS clause required for join: {context.GetText()}");

        var let = ListenerHelper.BuildLet(context.let_list());

        stage.Init(collection, asField, on, let, !pipelineExists || pipeline.Count == 0 ? null : pipeline);
        _currentPipeline.Add(stage);
    }

    public override void EnterFacet_def(Parser.Facet_defContext context)
    {
        var stage = new AstStageFacet();
        _stage.Push(stage);
    }

    public override void ExitFacet_def(Parser.Facet_defContext context)
    {
        var stage     = (AstStageFacet)_stage.Pop();
        var pipelines = new List<AstPipeline>{_currentPipeline};

        if (context.pipeline_def().Length != context.VARIABLE().Length)
            throw new($"context.pipeline_def().Length={context.pipeline_def().Length} != context.VARIABLE().Length={context.VARIABLE().Length}");

        if (_pipelines.Count <= context.pipeline_def().Length)
            throw new($"_pipelines.Count={_pipelines.Count} but context.pipeline_def().Length={context.pipeline_def().Length}");

        for (var i = 0; i < context.pipeline_def().Length-1; i++)
        {
            pipelines.Insert(0, _pipelines.Pop());
        }

        for (var i = 0; i < pipelines.Count; i++)
        {
            var name          = context.VARIABLE(i).GetText();
            var pipeline      = pipelines[i];
            var namedPipeline = new AstNamedPipeline(name, pipeline);
            
            stage.Add( namedPipeline );
        }

        if (_pipelines.Count < 1)
            throw new($"Invalid _pipelines Count={_pipelines.Count}");

        _currentPipeline = _pipelines.Pop();

        _currentPipeline.Add(stage);
    }

    public override void EnterDo_def([NotNull] Parser.Do_defContext context)
    {
        var stage = new AstStageDo();
        _stage.Push(stage);
    }

    public override void ExitDo_def([NotNull] Parser.Do_defContext context)
    {
        var stage = (AstStageDo)_stage.Pop();

        var json = JsonListenerHelper.Convert(context.json());
        stage.Json = json;

        _currentPipeline.Add(stage);
    }



}

