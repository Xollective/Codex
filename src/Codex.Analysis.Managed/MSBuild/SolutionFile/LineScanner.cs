// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Text;

namespace Codex.MSBuild
{
    internal class LineScanner
    {
        private readonly SubSourceText _line;
        private int _currentPosition;

        public LineScanner(SubSourceText line)
        {
            _line = line;
        }

        public SubSourceText ReadUpToAndEat(string delimiter)
        {
            int index = _line.IndexOf(delimiter, _currentPosition);

            if (index == -1)
            {
                return ReadRest();
            }
            else
            {
                var upToDelimiter = _line.Substring(_currentPosition, index - _currentPosition);
                _currentPosition = index + delimiter.Length;
                return upToDelimiter;
            }
        }

        public SubSourceText ReadRest()
        {
            var rest = _line.Substring(_currentPosition);
            _currentPosition = _line.Length;
            return rest;
        }
    }
}