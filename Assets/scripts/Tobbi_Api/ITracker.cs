//  -----------------------------------------------------------------------
//  <copyright file="ITracker.cs" University="UMC">
//   Copyright (c) 2025 UMC All rights reserved.
//  </copyright>
//  <author>Istiak Ahmed</author>
//  -----------------------------------------------------------------------

using System;
using System.Text;

namespace CoralVR
{
    public interface ITracker
    {
        StringBuilder InitiateTracker(StringBuilder csvLogger);
        void SaveToFile(String path, StringBuilder csvLogger);
    }
}