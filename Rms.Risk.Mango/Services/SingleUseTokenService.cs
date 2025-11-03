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
﻿namespace Rms.Risk.Mango.Services;

public class SingleUseTokenService : ISingleUseTokenService
{
    public static TimeSpan TokenValidityInterval = TimeSpan.FromSeconds( 30 );

    private readonly Lock _syncObject = new();
    private readonly List<Tuple<DateTime, string>> _tokens = [];

    public string GetSingleUseToken()
    {
        var guid = Guid.NewGuid().ToString().Replace( "-", "" );
        lock ( _syncObject )
        {
            _tokens.Add( Tuple.Create( DateTime.Now + TokenValidityInterval, guid ) );
        }

        return guid;
    }

    public bool CheckSingleUseToken( string token )
    {
        lock ( _syncObject )
        {
            var toDelete = new List<int>( _tokens.Count );
            var valid = false;
            var now = DateTime.Now;
            for ( var i = 0; i < _tokens.Count; i++ )
            {
                var (expireAt, guid) = _tokens[i];
                if ( expireAt >= now )
                {
                    if ( !valid && guid == token )
                    {
                        valid = true;
                        // token is one-off
                        toDelete.Add( i );
                    }
                }
                else
                {
                    // token expired
                    toDelete.Add( i );
                }
            }

            for ( var i = toDelete.Count-1; i >= 0; i-- )
                _tokens.RemoveAt( i );

            return valid;
        }
    }
}
