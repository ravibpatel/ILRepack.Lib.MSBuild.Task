using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ILRepacking;
using Microsoft.Build.Framework;

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
        private MessageImportance _messageImportance = MessageImportance.High; 

        #endregion

        #region Fields

        /// <summary>
        ///     Specifies a key file to sign the output assembly.
        /// </summary>
        public virtual string KeyFile
        {
            get => _keyFile;
            set => _keyFile = BuildPath(ConvertEmptyToNull(value));
        }

        /// <summary>
        ///     Specifies a key container to use.
        /// </summary>
        public virtual string KeyContainer
        {
            get => _keyContainer;
            set => _keyContainer = value;
        }

        /// <summary>
        ///     Specifies a log file to output log information.
        /// </summary>
        public virtual string LogFile
        {
            get => _logFile;
            set => _logFile = BuildPath(ConvertEmptyToNull(value));
        }

        /// <summary>
        ///     Specifies the log task messages importance (High by default).
        /// </summary>
        public virtual string LogImportance { 
            get => _messageImportance.ToString();
            set => Enum.TryParse(value, true, out _messageImportance); 
        }

        /// <summary>
        ///     Merges types with identical names into one.
        /// </summary>
        public virtual bool Union { get; set; }

        /// <summary>
        ///     Enable/disable symbol file generation.
        /// </summary>
        public virtual bool DebugInfo { get; set; }

        /// <summary>
        ///     Take assembly attributes from the given assembly file.
        /// </summary>
        public virtual string AttributeFile
        {
            get => _attributeFile;
            set => _attributeFile = BuildPath(ConvertEmptyToNull(value));
        }

        /// <summary>
        ///     Copy assembly attributes (by default only the primary assembly attributes are copied).
        /// </summary>
        public virtual bool CopyAttributes { get; set; }

        /// <summary>
        ///     Allows multiple attributes (if type allows).
        /// </summary>
        public virtual bool AllowMultiple { get; set; }

        /// <summary>
        ///     Target assembly kind (Exe|Dll|WinExe|SameAsPrimaryAssembly).
        /// </summary>
        public virtual string TargetKind
        {
            get => _targetKind.ToString();
            set
            {
                if (Enum.IsDefined(typeof(ILRepacking.ILRepack.Kind), value))
                {
                    _targetKind = (ILRepacking.ILRepack.Kind)Enum.Parse(typeof(ILRepacking.ILRepack.Kind), value);
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
        ///     Target platform (v1, v1.1, v2, v4 supported).
        /// </summary>
        public virtual string TargetPlatformVersion { get; set; }

        /// <summary>
        ///     Path of Directory where the target platform is located.
        /// </summary>
        public virtual string TargetPlatformDirectory { get; set; }

        /// <summary>
        ///     Merge assembly XML documentation.
        /// </summary>
        public bool XmlDocumentation { get; set; }

        /// <summary>
        ///     List of paths to use as "include directories" when attempting to merge assemblies.
        /// </summary>
        public virtual ITaskItem[] LibraryPath { get; set; } = new ITaskItem[0];

        /// <summary>
        ///     Set all types but the ones from the first assembly 'internal'.
        /// </summary>
        public virtual bool Internalize { get; set; }

        /// <summary>
        ///     Rename all internalized types (to be used when Internalize is enabled).
        /// </summary>
        public virtual bool RenameInternalized { get; set; }

        /// <summary>
        ///     Do not internalize types marked as Serializable.
        /// </summary>
        public virtual bool ExcludeInternalizeSerializable { get; set; }

        /// <summary>
        ///     If Internalize is set to true, any which match these regular expressions will not be internalized.
        ///     If Internalize is false, then this property is ignored.
        /// </summary>
        public virtual ITaskItem[] InternalizeExclude { get; set; }

        /// <summary>
        ///     List of specific assemblies to internalize.
        /// </summary>
        public virtual ITaskItem[] InternalizeAssembly { get; set; }

        /// <summary>
        ///     Output name for the merged assembly.
        /// </summary>
        [Required]
        public virtual string OutputFile
        {
            get => _outputFile;
            set => _outputFile = ConvertEmptyToNull(value);
        }

        /// <summary>
        ///     List of assemblies that will be merged.
        /// </summary>
        [Required]
        public virtual ITaskItem[] InputAssemblies { get; set; } = new ITaskItem[0];

        /// <summary>
        ///     Set the key file, but don't sign the assembly.
        /// </summary>
        public virtual bool DelaySign { get; set; }

        /// <summary>
        ///     Allows duplicating resources in the output assembly (by default they're ignored).
        /// </summary>
        public virtual bool AllowDuplicateResources { get; set; }

        /// <summary>
        ///     Allows the specified namespaces from being duplicated into input assemblies.
        ///     Multiple namespaces are delimited by ",".
        /// </summary>
        public virtual string AllowedDuplicateNamespaces { get; set; }

        /// <summary>
        ///     Allows assemblies with Zero PeKind (but obviously only IL will get merged).
        /// </summary>
        public virtual bool ZeroPeKind { get; set; }

        /// <summary>
        ///     Use as many CPUs as possible to merge the assemblies.
        /// </summary>
        public virtual bool Parallel { get; set; } = true;

        /// <summary>
        ///     Pause execution once completed (good for debugging).
        /// </summary>
        public virtual bool PauseBeforeExit { get; set; }

        /// <summary>
        ///     Additional debug information during the merge that will be outputted to LogFile.
        /// </summary>
        public virtual bool Verbose { get; set; }

        /// <summary>
        ///     Does not add the embedded resource 'ILRepack.List' with all merged assembly names.
        /// </summary>
        public virtual bool NoRepackRes { get; set; }

        /// <summary>
        ///     Allows (and resolves) file wildcards (e.g., `*.dll`) in input assemblies.
        /// </summary>
        public virtual bool Wildcards { get; set; }

        /// <summary>
        ///     Name of an attribute (optional). Members in input assemblies marked with this attribute will be dropped during merging.
        /// </summary>
        public virtual string RepackDropAttribute { get; set; }

        /// <summary>
        ///     Merge IL Linker file XML resources from Microsoft assemblies (optional). Same-named XML resources ('ILLink.*.xml') will be combined during merging.
        /// </summary>
        public virtual bool MergeIlLinkerFiles { get; set; }

        #endregion

        #region Public methods

        /// <summary>
        ///     Executes ILRepack with specified options.
        /// </summary>
        /// <returns>Returns true if it's successful.</returns>
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
                ExcludeInternalizeSerializable = ExcludeInternalizeSerializable,
                DelaySign = DelaySign,
                AllowDuplicateResources = AllowDuplicateResources,
                AllowZeroPeKind = ZeroPeKind,
                Parallel = Parallel,
                PauseBeforeExit = PauseBeforeExit,
                OutputFile = _outputFile,
                AllowWildCards = Wildcards,
                RepackDropAttribute = RepackDropAttribute,
                MergeIlLinkerFiles = MergeIlLinkerFiles
            };

            repackOptions.AllowedDuplicateNameSpaces.AddRange(
                ParseDuplicateNamespacesOption(AllowedDuplicateNamespaces));

            var logger = new Logger
            {
                ShouldLogVerbose = repackOptions.LogVerbose
            };

            try
            {
                // Attempt to create output directory if it does not exist.
                string outputPath = Path.GetDirectoryName(OutputFile);
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
                for (var i = 0; i < InputAssemblies.Length; i++)
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

                    LogMessage("Added assembly '{0}'", assemblies[i]);
                }

                // List of regex to compare against FullName of types NOT to internalize
                if (InternalizeExclude != null)
                {
                    var internalizeExclude = new string[InternalizeExclude.Length];
                    if (Internalize)
                    {
                        for (var i = 0; i < InternalizeExclude.Length; i++)
                        {
                            internalizeExclude[i] = InternalizeExclude[i].ItemSpec;
                            if (string.IsNullOrEmpty(internalizeExclude[i]))
                            {
                                throw new Exception(
                                    $"Invalid internalize exclude pattern at item index {i}. Pattern cannot be blank.");
                            }

                            LogMessage(
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

                if (InternalizeAssembly != null && InternalizeAssembly.Any())
                {
                    repackOptions.InternalizeAssemblies = InternalizeAssembly.Select(i => StripExtension(i.ItemSpec)).ToArray();
                }

                if (!string.IsNullOrWhiteSpace(repackOptions.RepackDropAttribute))
                {
                    repackOptions.RepackDropAttributes.UnionWith(RepackDropAttribute.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
                }

                // Path that will be used when searching for assemblies to merge.
                var searchPath = new List<string> { "." };
                searchPath.AddRange(LibraryPath.Select(iti => BuildPath(iti.ItemSpec)));
                repackOptions.SearchDirectories = searchPath.ToArray();

                // Attempt to merge assemblies.
                LogMessage("Merging {0} assembl{1} to '{2}'",
                    InputAssemblies.Length, InputAssemblies.Length != 1 ? "ies" : "y", _outputFile);

                // Measure performance
                var stopWatch = new Stopwatch();
                stopWatch.Start();

                if (logger.Open(repackOptions.LogFile))
                {
                    repackOptions.Log = true;
                }

                var ilRepack = new ILRepacking.ILRepack(repackOptions, logger);
                ilRepack.Repack();

                stopWatch.Stop();

                LogMessage("Merge succeeded in {0} s", stopWatch.Elapsed.TotalSeconds);
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

        /// <summary>
        /// Logs a message with the specified format and arguments.
        /// </summary>
        /// <param name="message">The message format.</param>
        /// <param name="messageArgs">The arguments for the message format.</param>
        private void LogMessage(string message, params object[] messageArgs)
        {
            Log.LogMessage(_messageImportance, message, messageArgs);
        }

        /// <summary>
        ///     Parses the command line options for AllowedDuplicateNameSpaces.
        /// </summary>
        /// <param name="value">The given options.</param>
        /// <returns>A collection of all allowed namespace duplicates.</returns>
        private static IEnumerable<string> ParseDuplicateNamespacesOption(string value)
        {
            return string.IsNullOrEmpty(value) ? Array.Empty<string>() : value.Split(',');
        }

        /// <summary>
        ///     Converts empty string to null.
        /// </summary>
        /// <param name="str">String to check for emptiness</param>
        /// <returns></returns>
        private static string ConvertEmptyToNull(string str)
        {
            return string.IsNullOrEmpty(str) ? null : str;
        }

        /// <summary>
        ///     Returns path respective to current working directory.
        /// </summary>
        /// <param name="path">Relative path to current working directory</param>
        /// <returns></returns>
        private static string BuildPath(string path)
        {
            string workDir = Directory.GetCurrentDirectory();
            return string.IsNullOrEmpty(path) ? null : Path.Combine(workDir, path);
        }

        /// <summary>
        ///     Strips .dll or .exe extension from filePath if present.
        /// </summary>
        /// <param name="filePath">File path</param>
        /// <returns>File path without the extension.</returns>
        private static string StripExtension(string filePath)
        {
            if (filePath == null)
            {
                return null;
            }

            if (filePath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) || filePath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                filePath = filePath.Substring(0, filePath.Length - 4);
            }

            return filePath;
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
