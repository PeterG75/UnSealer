﻿using AsmResolver.DotNet;
using AsmResolver.DotNet.Builder;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.PE.DotNet.Builder;
using dnlib.DotNet;
using dnlib.DotNet.Writer;
using System.Reflection;

namespace UnSealer.Core
{
    public class Context
    {
        /// <summary>
        /// Initialize For Context
        /// </summary>
        /// <param name="Location"> Module Path </param>
        /// <param name="Logger"> Logger's We Created On MainWindow ;D </param>
        public Context(string Location, Logger[] Logger)
        {
            PathIs = Location;
            AsmModule = ModuleDefinition.FromFile(PathIs);
            DnModule = ModuleDefMD.Load(PathIs);
            Log = Logger[0];
            StringLog = Logger[1];
                                                                                       // Due To Some UnManged methods :(
            try { SysModule = Assembly.UnsafeLoadFrom(PathIs).ManifestModule; } catch { Log.Error("Failed Maintaining Reflection Module !"); }
        }
        /// <summary>
        /// Saves Assembly After Modifications
        /// </summary>
        public void SaveContext()
        {
            string NewPath = PathIs.Insert(PathIs.Length - 4, "HereWeGo"); // Thx 4 drakoniа#0601 for the insert trick :D
            if (DnModule != null)
            {
                if (DnModule.IsILOnly)
                {
                    var MangedWriter = new ModuleWriterOptions(DnModule)
                    {
                        Logger = DummyLogger.NoThrowInstance,
                        MetadataOptions = { Flags = MetadataFlags.PreserveAll }
                    };
                    DnModule.Write(NewPath.Replace("HereWeGo", "-DnLibed"), MangedWriter);
                    Log.Info("Done Saved Manged Dnlib Module");
                }
                else
                {
                    var UnMangedWriter = new NativeModuleWriterOptions(DnModule, false)
                    {
                        Logger = DummyLogger.NoThrowInstance,
                        MetadataOptions = { Flags = MetadataFlags.PreserveAll }
                    };
                    DnModule.NativeWrite(NewPath.Replace("HereWeGo", "-DnLibed"), UnMangedWriter);
                    Log.Info("Done Saved Native Dnlib Module");
                }
            }
            if (AsmModule != null)
            {
                var IMPEIB = new ManagedPEImageBuilder()
                {
                    DotNetDirectoryFactory = new DotNetDirectoryFactory() 
                    {
                        MetadataBuilderFlags = MetadataBuilderFlags.PreserveAll,
                        MethodBodySerializer = new CilMethodBodySerializer 
                        {
                            ComputeMaxStackOnBuildOverride = false 
                        }
                    }
                };
                var IR = IMPEIB.CreateImage(AsmModule);
                var FBuilder = new ManagedPEFileBuilder();
                var File = FBuilder.CreateFile(IR.ConstructedImage);
                if (!IR.DiagnosticBag.IsFatal)
                    File.Write(NewPath.Replace("HereWeGo", "-AsmResolved")); // Ignore Errors.
                else
                    AsmModule.Write(NewPath.Replace("HereWeGo", "-AsmResolved"), IMPEIB);
                Log.Info("Done Saved AsmResolver Module");
            }
        }
        /// <summary>
        /// Module In Dnlib Way
        /// </summary>
        public ModuleDefMD DnModule { set; get; }
        /// <summary>
        /// Module In AsmReslover Way
        /// </summary>
        public ModuleDefinition AsmModule { set; get; }
        /// <summary>
        /// Module In Reflection Way ( For Plugins Who Uses Invoke etc. )
        /// </summary>
        public Module SysModule { set; get; }
        /// <summary>
        /// Logger.
        /// </summary>
        public Logger Log { set; get; }
        /// <summary>
        /// String Logger.
        /// </summary>
        public Logger StringLog { set; get; }
        /// <summary>
        /// Path Loaded On Initialize Constructor
        /// </summary>
        private string PathIs { set; get; }
    }
}