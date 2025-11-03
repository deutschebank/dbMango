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
namespace Rms.Risk.Mango.Pivot.Core.Models;

public class Navigation<T>(Action<PivotDefinition, T> apply) where T : class
{

    private class NavigationRecord
    {
        public PivotDefinition? Def  { get; init; }
        public T?               Data { get; init; }

        public override string ToString() => $"{Def?.Name} Hash={Def?.GetHashCode():X}";
    }

    private readonly Action<PivotDefinition,T> _apply        = apply;
    private readonly Stack<NavigationRecord>   _backStack    = new();
    private readonly Stack<NavigationRecord>   _forwardStack = new();
    private          NavigationRecord?         _current;

    public bool CanGoBackward => _backStack.Count > 0;
    public bool CanGoForward  => _forwardStack.Count > 0;

    public void Add( PivotDefinition defToAdd, T data )
    {
        var rec = new NavigationRecord
        {
            Def  = defToAdd,
            Data = data
        };
        if ( _current != null )
            _backStack.Push( _current );
        _current = rec;
        _forwardStack.Clear();
    }

    public void Clear()
    {
        _current = null;
        _backStack.Clear();
        _forwardStack.Clear();
    }

    public bool Back()
    {
        if (_backStack.Count == 0)
            return false;

        var rec = _backStack.Pop();
        if ( _current != null )
            _forwardStack.Push( _current );
        Apply( rec );

        return true;
    }

    public bool Forward()
    {
        if (_forwardStack.Count == 0)
            return false;

        var rec = _forwardStack.Pop();
        if ( _current != null )
            _backStack.Push( rec );

        Apply( rec);

        return true;
    }

    private void Apply( NavigationRecord rec )
    {
        _current = rec;
        _apply(rec.Def!, rec.Data!);
    }
}