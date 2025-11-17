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
using Blazored.Modal;
using Blazored.Modal.Services;
using Rms.Risk.Mango.Pivot.UI.Services;

namespace Rms.Risk.Mango.Pivot.UI.Controls;

public static class ModalDialogUtils
{
    private static ILogger? _logger;

    public static void SetLogger(ILogger logger) => _logger = logger;

    /// <summary>
    /// Display a user information dialog box, allows the user to click ok or cancel
    /// </summary>
    public static async Task<ModalResult> ShowConfirmationDialog(
        IModalService               service, 
        string                      header, 
        string                      message, 
        Dictionary<string, string>? info                 = null, 
        Dictionary<string, string>? additionalParams     = null,
        string?                     inputPrompt          = null,
        bool                        isInputTextMandatory = false
        )
    {
        var parameters = new ModalParameters { { "Text", message } };
        if (info != null)
            parameters.Add("Info", info);
        parameters.Add("ShowCancel", true);

        if (!string.IsNullOrEmpty(inputPrompt))
        {
            parameters.Add("ShowInputText",        true);
            parameters.Add("InputTextLabel",       inputPrompt);
            parameters.Add("IsInputTextMandatory", isInputTextMandatory);
        }

        if (additionalParams != null)
        {
            foreach (KeyValuePair<string, string> parameter in additionalParams)
            {
                parameters.Add(parameter.Key, parameter.Value);
            }
        }

        var options = new ModalOptions
        {
            HideCloseButton         = true,
            DisableBackgroundCancel = true
        };

        _logger?.LogInformation($"Confirmation: {header} - {message}");
        var form = service.Show<MessageBoxKeyValueComponent>(header, parameters, options);
        return await form.Result;
    }

    /// <summary>
    /// Display a user information dialog box with checkboxes
    /// </summary>
    /// <param name="service"></param>
    /// <param name="header"></param>
    /// <param name="message"></param>
    /// <param name="info"></param>
    /// <returns></returns>
    public static async Task<ModalResult> ShowConfirmationDialog(IModalService service, string header, string message, Dictionary<string, bool> info)
    {
        var parameters = new ModalParameters { { "Text", message } };
        var checkboxes = info
                        .Select(x => new MessageBoxCheckBoxesComponent.BoolNameValue { Name = x.Key, Value = x.Value })
                        .ToList();
        parameters.Add("Info", checkboxes);
        parameters.Add("ShowCancel", true);

        var options = new ModalOptions
        {
            HideCloseButton         = true,
            DisableBackgroundCancel = true
        };

        _logger?.LogInformation($"Confirmation: {header} - {message}");
        var form = service.Show<MessageBoxCheckBoxesComponent>(header, parameters, options);
        var res = await form.Result;

        if ( res.Confirmed )
        {
            foreach ( var boolNameValue in checkboxes )
            {
                info[boolNameValue.Name] = boolNameValue.Value;
            }
        }

        return res;
    }

    /// <summary>
    /// Display a query dialog box, allows the user to enter some text and click ok or cancel
    /// </summary>
    /// <param name="service"></param>
    /// <param name="header"></param>
    /// <param name="message"></param>
    /// <param name="initialInput"></param>
    /// <param name="info"></param>
    /// <param name="inputLabel"></param>
    /// <returns>Input text or null if cancelled</returns>
    public static async Task<string?> ShowConfirmationDialogWithInput(
        IModalService               service, 
        string                      header, 
        string                      message, 
        string?                     inputLabel   = null,
        string?                     initialInput = null,
        Dictionary<string, string>? info         = null)
    {
        var parameters = new ModalParameters
        {
            { nameof(MessageBoxKeyValueComponent.Text), message },
            { nameof(MessageBoxKeyValueComponent.ShowInputText), true }
        };
        if (info != null)
            parameters.Add(nameof(MessageBoxKeyValueComponent.Info), info);
        if (initialInput != null)
            parameters.Add(nameof(MessageBoxKeyValueComponent.InputText), initialInput);
        if (inputLabel != null)
            parameters.Add(nameof(MessageBoxKeyValueComponent.InputTextLabel), inputLabel);
        parameters.Add(nameof(    MessageBoxKeyValueComponent.ShowCancel), true);

        var options = new ModalOptions
        {
            HideCloseButton         = true,
            DisableBackgroundCancel = true
        };

        _logger?.LogInformation($"Confirmation: {header} - {message}");
        var form = service.Show<MessageBoxKeyValueComponent>(header, parameters, options);
        var res = await form.Result;
        return res.Cancelled ? null : res.Data?.ToString();
    }

    /// <summary>
    /// Display a user information dialog box, just shows the ok button
    /// </summary>
    /// <param name="service"></param>
    /// <param name="header"></param>
    /// <param name="message"></param>
    /// <param name="info"></param>
    /// <returns></returns>
    public static async Task ShowInfoDialog(IModalService service, string header, string message, Dictionary<string, string?>? info = null)
    {
        var parameters = new ModalParameters { { "Text", message } };
        if (info != null)
            parameters.Add("Info", info);
        parameters.Add("ShowCancel", false);

        var options = new ModalOptions
        {
            HideCloseButton         = true,
            DisableBackgroundCancel = true
        };

        _logger?.LogInformation($"Info: {header} - {message}");
        var form = service.Show<MessageBoxKeyValueComponent>(header, parameters, options);
        await form.Result;
    }


    /// <summary>
    /// Display a user information dialog box, just shows the ok button
    /// </summary>
    /// <param name="service"></param>
    /// <param name="header"></param>
    /// <param name="text"></param>
    /// <param name="message"></param>
    /// <param name="mimeType"></param>
    /// <returns></returns>
    public static async Task ShowTextDialog(IModalService service, string header, string text, string? message = null, string mimeType="application/json")
    {
        var parameters = new ModalParameters { { "ShowCancel", false } };
        if ( !string.IsNullOrWhiteSpace(text))
            parameters.Add("Text"            , text);
        if ( !string.IsNullOrWhiteSpace(message))
            parameters.Add("Message"         , message);
        if ( !string.IsNullOrWhiteSpace(mimeType))
            parameters.Add("MimeType", mimeType);

        var options = new ModalOptions
        {
            HideCloseButton         = true,
            DisableBackgroundCancel = true
        };

        var form = service.Show<MessageBoxTextComponent>(header, parameters, options);
        await form.Result;
    }

    /// <summary>
    /// Show a model error dialog when an exception was thrown, just shows an ok button
    /// </summary>
    /// <param name="service"></param>
    /// <param name="header"></param>
    /// <param name="e"></param>
    /// <returns></returns>
    public static async Task ShowExceptionDialog(IModalService service, string header, Exception e)
    {
        var parameters = new ModalParameters
        {
            { "Message",   header },
            { "Exception", e      }
        };

        var options = new ModalOptions
        {
            HideCloseButton         = false,
            DisableBackgroundCancel = false
        };

        _logger?.LogError(e, $"{header} - {e.Message}");
        var form = service.Show<MessageBoxExceptionComponent>(header, parameters, options);
        await form.Result;
    }

    /// <summary>
    /// Make async call UI-safe. Shows dialog if exception happen
    /// </summary>
    /// <param name="service"></param>
    /// <param name="header"></param>
    /// <param name="action"></param>
    /// <returns></returns>
    public static async Task SafeCall(IModalService service, string header, Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            await ShowExceptionDialog(service, header, ex);
        }
    }

    /// <summary>
    /// Make UI-safe async call and show its progress in the modal dialog.
    /// </summary>
    public static async Task ShowProgress(IModalService service, string header, Func<ActionProgressCallback, Task> action)
    {
        var parameters = new ModalParameters
        {
            { "Action", action },
        };

        var options = new ModalOptions
        {
            HideCloseButton         = true,
            DisableBackgroundCancel = true
        };

        var form = service.Show<MessageBoxProgressComponent>(header, parameters, options);
        await form.Result;
    }
}
