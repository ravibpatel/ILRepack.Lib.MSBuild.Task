/**
 * Copyright (c) 2004, Evain Jb (jb@evain.net)
 * Modified 2007 Marcus Griep (neoeinstein+boo@gmail.com)
 * Modified 2013 Peter Sunde (peter.sunde@gmail.com)
 * Modified 2016-2019 Ravi Patel (www.rbsoft.org)
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *
 *     - Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     - Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     - Neither the name of Evain Jb nor the names of its contributors may
 *       be used to endorse or promote products derived from this software
 *       without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE
 * LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
 * SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
 * CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
 * POSSIBILITY OF SUCH DAMAGE.
 *
 *****************************************************************************/

using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using ILRepacking;
using Microsoft.Build.Framework;
using System.Diagnostics;

namespace ILRepack.Lib.MSBuild.Task
{
    public class ILRepack : Microsoft.Build.Utilities.Task, IDisposable
    {
        #region Variables

        private string _attributeFile;
        private string _logFile;
        private string _outputFile;
        private string _keyFile;
        private string _keyContainer;
        private ILRepacking.ILRepack.Kind _targetKind;
        private string _excludeFileTmpPath;

        #endregion

        #region Fields

        /// <summary>
        /// Specifies a keyfile to sign the output assembly.
        /// </summary>
        public virtual string KeyFile
        {
            get => _keyFile;
            set => _keyFile = BuildPath(ConvertEmptyToNull(value));
        }

        /// <summary>
        /// Specifies a KeyContainer to use.
        /// </summary>
        public virtual string KeyContainer
        {
            get => _keyContainer;
            set => _keyContainer = value;
        }

        /// <summary>
        /// Specifies a logfile to output log information.
        /// </summary>
        public virtual string LogFile
        {
            get => _logFile;
            set => _logFile = BuildPath(ConvertEmptyToNull(value));
        }

        /// <summary>
        /// Merges types with identical names into one.
        /// </summary>
        public virtual bool Union { get; set; }

        /// <summary>
        /// Enable/disable symbol file generation.
        /// </summary>
        public virtual bool DebugInfo { get; set; }

        /// <summary>
        /// Take assembly attributes from the given assembly file.
        /// </summary>
        public virtual string AttributeFile
        {
            get => _attributeFile;
            set => _attributeFile = BuildPath(ConvertEmptyToNull(value));
        }

        /// <summary>
        /// Copy assembly attributes (by default only the primary assembly attributes are copied).
        /// </summary>
        public virtual bool CopyAttributes { get; set; }

        /// <summary>
        /// Allows multiple attributes (if type allows).
        /// </summary>
        public virtual bool AllowMultiple { get; set; }

        /// <summary>
        /// Target assembly kind (Exe|Dll|WinExe|SameAsPrimaryAssembly).
        /// </summary>
        public virtual string TargetKind
        {
            get => _targetKind.ToString();
            set
            {
                if (Enum.IsDefined(typeof(ILRepacking.ILRepack.Kind), value))
                {
                    _targetKind = (ILRepacking.ILRepack.Kind) Enum.Parse(typeof(ILRepacking.ILRepack.Kind), value);
                }
                else
                {
                    Log.LogWarning("TargetKind should be [Exe|Dll|" +
                                   "WinExe|SameAsPrimaryAssembly]; " +
                                   "set to SameAsPrimaryAssembly");
                    _targetKind = ILRepacking.ILRepack.Kind.SameAsPrimaryAssembly;
                }
            }
        }

        /// <summary>
        /// Target platform (v1, v1.1, v2, v4 supported).
        /// </summary>
        public virtual string TargetPlatformVersion { get; set; }

        /// <summary>
        /// Path of Directory where target platform is located.
        /// </summary>
        public virtual string TargetPlatformDirectory { get; set; }

        /// <summary>
        /// Merge assembly xml documentation.
        /// </summary>
        public bool XmlDocumentation { get; set; }

        /// <summary>
        /// List of paths to use as "include directories" when attempting to merge assemblies.
        /// </summary>
        public virtual ITaskItem[] LibraryPath { get; set; } = new ITaskItem[0];

        /// <summary>
        /// Set all types but the ones from the first assembly 'internal'.
        /// </summary>
        public virtual bool Internalize { get; set; }

        /// <summary>
        /// Rename all internalized types (to be used when Internalize is enabled).
        /// </summary>
        public virtual bool RenameInternalized { get; set; }

        /// <summary>
        /// If Internalize is set to true, any which match these regular expressions will not be internalized. 
        /// If internalize is false, then this property is ignored.
        /// </summary>
        public virtual ITaskItem[] InternalizeExclude { get; set; }

        /// <summary>
        /// Output name for merged assembly.
        /// </summary>
        [Required]
        public virtual string OutputFile
        {
            get => _outputFile;
            set => _outputFile = ConvertEmptyToNull(value);
        }

        /// <summary>
        /// List of assemblies that will be merged.
        /// </summary>
        [Required]
        public virtual ITaskItem[] InputAssemblies { get; set; } = new ITaskItem[0];

        /// <summary>
        /// Set the keyfile, but don't sign the assembly.
        /// </summary>
        public virtual bool DelaySign { get; set; }

        /// <summary>
        /// Allows to duplicate resources in output assembly (by default they're ignored).
        /// </summary>
        public virtual bool AllowDuplicateResources { get; set; }

        /// <summary>
        /// Allows the specified namespaces for being duplicated in to input assemblies.
        /// </summary>
        public virtual string AllowedDuplicateNamespaces { get; set; }

        /// <summary>
        /// Allows assemblies with Zero PeKind (but obviously only IL will get merged).
        /// </summary>
        public virtual bool ZeroPeKind { get; set; }

        /// <summary>
        /// Use as many CPUs as possible to merge the assemblies.
        /// </summary>
        public virtual bool Parallel { get; set; } = true;

        /// <summary>
        /// Pause execution once completed (good for debugging).
        /// </summary>
        public virtual bool PauseBeforeExit { get; set; }

        /// <summary>
        /// Additional debug information during merge that will be outputted to LogFile.
        /// </summary>
        public virtual bool Verbose { get; set; }

        /// <summary>
        /// Does not add the embedded resource 'ILRepack.List' with all merged assembly names.
        /// </summary>
        public virtual bool NoRepackRes { get; set; }

        /// <summary>
        /// Allows (and resolves) file wildcards (e.g. `*`.dll) in input assemblies.
        /// </summary>
        public virtual bool Wildcards { get; set; }

        /// <summary>
        /// Name of an attribute. Members in InputAssemblies marked with this attribute will be
        /// dropped during merging.
        /// </summary>
        public string RepackDropAttribute { get; set; }
        #endregion

        #region Public methods

        /// <summary>
        ///     Executes ILRepack with specified options.
        /// </summary>
        /// <returns>Returns true if its successful.</returns>
        public override bool Execute()
        {
            var repackOptions = new RepackOptions
            {
                KeyFile = _keyFile,
                KeyContainer = _keyContainer,
                LogFile = _logFile,
                LogVerbose = Verbose,
                NoRepackRes = NoRepackRes,
                UnionMerge = Union,
                DebugInfo = DebugInfo,
                CopyAttributes = CopyAttributes,
                AttributeFile = AttributeFile,
                AllowMultipleAssemblyLevelAttributes = AllowMultiple,
                TargetKind = _targetKind,
                TargetPlatformVersion = TargetPlatformVersion,
                TargetPlatformDirectory = TargetPlatformDirectory,
                XmlDocumentation = XmlDocumentation,
                Internalize = Internalize,
                RenameInternalized = RenameInternalized,
                DelaySign = DelaySign,
                AllowDuplicateResources = AllowDuplicateResources,
                AllowZeroPeKind = ZeroPeKind,
                Parallel = Parallel,
                PauseBeforeExit = PauseBeforeExit,
                OutputFile = _outputFile,
                AllowWildCards = Wildcards,
                RepackDropAttribute = RepackDropAttribute
            };

            repackOptions.AllowedDuplicateNameSpaces.AddRange(ParseDuplicateNamespacesOption(AllowedDuplicateNamespaces));

            Logger logger = new Logger
            {
                ShouldLogVerbose = repackOptions.LogVerbose
            };

            try
            {
                // Attempt to create output directory if it does not exist.
                var outputPath = Path.GetDirectoryName(OutputFile);
                if (outputPath != null && !Directory.Exists(outputPath))
                {
                    try
                    {
                        Directory.CreateDirectory(outputPath);
                    }
                    catch (Exception ex)
                    {
                        Log.LogErrorFromException(ex);
                        return false;
                    }
                }

                // Assemblies to be merged.
                var assemblies = new string[InputAssemblies.Length];
                for (int i = 0; i < InputAssemblies.Length; i++)
                {
                    assemblies[i] = InputAssemblies[i].ItemSpec;
                    if (string.IsNullOrEmpty(assemblies[i]))
                    {
                        throw new Exception($"Invalid assembly path on item index {i}");
                    }

                    if (!File.Exists(assemblies[i]) && !File.Exists(BuildPath(assemblies[i])))
                    {
                        throw new Exception($"Unable to resolve assembly '{assemblies[i]}'");
                    }

                    Log.LogMessage(MessageImportance.High, "Added assembly '{0}'", assemblies[i]);
                }

                // List of regex to compare against FullName of types NOT to internalize
                if (InternalizeExclude != null)
                {
                    var internalizeExclude = new string[InternalizeExclude.Length];
                    if (Internalize)
                    {
                        for (int i = 0; i < InternalizeExclude.Length; i++)
                        {
                            internalizeExclude[i] = InternalizeExclude[i].ItemSpec;
                            if (string.IsNullOrEmpty(internalizeExclude[i]))
                            {
                                throw new Exception(
                                    $"Invalid internalize exclude pattern at item index {i}. Pattern cannot be blank.");
                            }

                            Log.LogMessage(MessageImportance.High,
                                "Excluding namespaces/types matching pattern '{0}' from being internalized",
                                internalizeExclude[i]);
                        }

                        // Create a temporary file with a list of assemblies that should not be internalized.
                        _excludeFileTmpPath = Path.GetTempFileName();
                        File.WriteAllLines(_excludeFileTmpPath, internalizeExclude);
                        repackOptions.ExcludeFile = _excludeFileTmpPath;
                    }
                }

                repackOptions.InputAssemblies = assemblies;

                // Path that will be used when searching for assemblies to merge.
                var searchPath = new List<string> {"."};
                searchPath.AddRange(LibraryPath.Select(iti => BuildPath(iti.ItemSpec)));
                repackOptions.SearchDirectories = searchPath.ToArray();

                // Attempt to merge assemblies.
                Log.LogMessage(MessageImportance.High, "Merging {0} assemb{1} to '{2}'",
                    InputAssemblies.Length, InputAssemblies.Length != 1 ? "ies" : "y", _outputFile);

                // Measure performance
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();

                if (logger.Open(repackOptions.LogFile))
                {
                    repackOptions.Log = true;
                }

                ILRepacking.ILRepack ilRepack = new ILRepacking.ILRepack(repackOptions, logger);
                ilRepack.Repack();

                stopWatch.Stop();

                Log.LogMessage(MessageImportance.High, "Merge succeeded in {0} s", stopWatch.Elapsed.TotalSeconds);
                logger.Close();

                return true;
            }
            catch (Exception e)
            {
                logger.Log(e);
                logger.Close();
                Log.LogErrorFromException(e);
                return false;
            }
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Parses the command line options for AllowedDuplicateNameSpaces.
        /// </summary>
        /// <param name="value">The given options.</param>
        /// <returns>A collection of all allowed namespace duplicates.</returns>
        private static IEnumerable<string> ParseDuplicateNamespacesOption(string value)
        {
            if (string.IsNullOrEmpty(value)) return new string[0];

            return value.Split(',');
        }

        /// <summary>
        /// Converts empty string to null.
        /// </summary>
        /// <param name="str">String to check for emptiness</param>
        /// <returns></returns>
        private static string ConvertEmptyToNull(string str)
        {
            return string.IsNullOrEmpty(str) ? null : str;
        }

        /// <summary>
        /// Returns path respective to current working directory.
        /// </summary>
        /// <param name="path">Relative path to current working directory</param>
        /// <returns></returns>
        private string BuildPath(string path)
        {
            var workDir = Directory.GetCurrentDirectory();
            return string.IsNullOrEmpty(path) ? null : Path.Combine(workDir, path);
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            // Remove temporary exclude file
            if (File.Exists(_excludeFileTmpPath))
            {
                File.Delete(_excludeFileTmpPath);
            }
        }

        #endregion
    }
}
