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
using System.ComponentModel;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace Rms.Risk.Mango.Pivot.Core.Models;

public interface IMutable
{
    bool IsBusy { get; }
    void Mute();
    void Unmute();
}

public class NotifyPropertyChangedBase : INotifyPropertyChanged, IMutable
{
    private static Action<Action> _invokeWrapper = action => action?.Invoke();

    /// <summary>
    /// For WPF applications call:
    /// NotifyPropertyChangedBase.SetInvokeWrapper(  Application.Current.Dispatcher.Invoke );
    /// </summary>
    /// <param name="invokeWrapper"></param>
    public static void SetInvokeWrapper(Action<Action> invokeWrapper)
        => _invokeWrapper = invokeWrapper;

#region INotifyPropertyChanged Members

    public event PropertyChangedEventHandler? PropertyChanged;

#endregion

    /// <summary>
    /// Returns the name of a property dynamically using an expression tree without requiring an instance
    /// usage: GetPropertyName@gt;MyClass@lt;( (MyClass c) => c.MyProperty) returns "MyProperty"
    /// </summary>
    public static string GetPropertyName<TModel, TProperty>(Expression<Func<TModel, TProperty>> property)
    {
        var              lambda = (LambdaExpression)property;
        MemberExpression memberExpression;

        if (lambda.Body is UnaryExpression body)
        {
            var unaryExpression = body;
            memberExpression = (MemberExpression)unaryExpression.Operand;
        }
        else
        {
            memberExpression = (MemberExpression)lambda.Body;
        }

        return memberExpression.Member.Name;
    }

    /// <summary>
    /// Returns the name of a property dynamically using an expression tree
    /// usage: GetPropertyName( () => someinstance.MyProperty) returns "MyProperty"
    /// </summary>
    public static string GetPropertyName<T>(Expression<Func<T>> property)
    {
        var              lambda = (LambdaExpression)property;
        MemberExpression memberExpression;

        if (lambda.Body is UnaryExpression body)
        {
            var unaryExpression = body;
            memberExpression = (MemberExpression)unaryExpression.Operand;
        }
        else
        {
            memberExpression = (MemberExpression)lambda.Body;
        }

        return memberExpression.Member.Name;
    }

    /// <summary>
    /// Overriden by subclasses, which may way want to to enable/diable the notification event.
    /// Called before every event notification
    /// </summary>
    /// <returns></returns>
    protected virtual bool AllowPropertyChanged() => true;

    /// <summary>
    /// Fires the property changed event
    /// Dynamically generates the property name using an expression tree
    /// usage: OnPropertyChanged( (MyClass m) => m.MyProperty)
    /// </summary>
    protected void OnPropertyChanged<TModel, TProperty>(Expression<Func<TModel, TProperty>> property)
    {
        var e = new PropertyChangedEventArgs(GetPropertyName(property));
        OnPropertyChanged(e);
    }


    /// <summary>
    /// Fires the property changed event
    /// Dynamically generates the property name using an expression tree
    /// usage: OnPropertyChanged( () => MyProperty)
    /// </summary>
    protected void OnPropertyChanged<T>(Expression<Func<T>> property)
    {
        var e = new PropertyChangedEventArgs(GetPropertyName(property));
        OnPropertyChanged(e);
    }

    /// <summary>
    /// Fire the notify property changed event with a static property name
    /// No not supply propName as it'll be automatically filled in
    /// </summary>
    protected void OnPropertyChanged([CallerMemberName] string propName = "")
    {
        var e = new PropertyChangedEventArgs(propName);
        OnPropertyChanged(e);
    }

    private void OnPropertyChanged(PropertyChangedEventArgs e)
    {
//            if (IsBusy)
//                return;

        var pc = PropertyChanged;
        if (pc != null && AllowPropertyChanged())
        {
            _invokeWrapper( () => pc(this, e) );
        }
    }

    public bool IsBusy
    {
        get => _muteCount > 0;
        set
        {
            if (value)
                Mute();
            else
                Unmute();
        }
    }

    private int _muteCount;

    public virtual void Mute()
    {
        _muteCount += 1;
        PropertyChanged?.Invoke(this, new(nameof(IsBusy)));
    }

    public virtual void Unmute()
    {
        _muteCount -= 1;
        PropertyChanged?.Invoke(this, new(nameof(IsBusy)));
        if (_muteCount < 0)
        {
#if DEBUG
            throw new ApplicationException("Mute count becomes negative");
#else
                _muteCount = 0;
#endif
        }
    }
}