using System;
using System.Collections.Generic;
using System.Linq;
using Cake.Core;
using Cake.Core.IO;
using Cake.Core.Tooling;

namespace Cake.Storm.Android
{
	internal class ZipAlignCommand : Tool<ToolSettings>
	{
		private readonly ICakeContext _context;

		internal ZipAlignCommand(ICakeContext context) : base(context.FileSystem, context.Environment, context.ProcessRunner, context.Tools)
		{
			_context = context;
		}

		protected override string GetToolName() => "zipalign";

		protected override IEnumerable<string> GetToolExecutableNames() => new[] { "zipalign" };

		protected override IEnumerable<FilePath> GetAlternativeToolPaths(ToolSettings settings)
		{
			if (_context.Environment.Platform.Family == PlatformFamily.Windows)
			{
				//find it in android sdk
				string programFiles = _context.Environment.GetSpecialPath(SpecialPath.ProgramFiles).FullPath;
				string programFilesX86 = _context.Environment.GetSpecialPath(SpecialPath.ProgramFilesX86).FullPath;

				return new[] {
					new FilePath("zipalign.exe")
				}.Concat(_context.Globber.GetFiles($"{programFiles}/Android/android-sdk/build-tools/*/zipalign.exe"))
				 .Concat(_context.Globber.GetFiles($"{programFilesX86}/Android/android-sdk/build-tools/*/zipalign.exe"));
			}
			else if (_context.Environment.Platform.Family == PlatformFamily.OSX)
			{
				string home = _context.Environment.GetEnvironmentVariable("HOME");
				return new[] {
					new FilePath("zipalign"),
				}.Concat(_context.Globber.GetFiles("/Library/Developer/Xamarin/android-sdk-macosx/build-tools/*/zipalign"))
				 .Concat(_context.Globber.GetFiles($"{home}/Library/Developer/Xamarin/android-sdk-macosx/build-tools/*/zipalign"));
			}
			else
			{
				throw new CakeException($"Environment {_context.Environment.Platform} not supported, only Windows and OSX are supported");
			}
		}

		public bool Align(FilePath inputApk, FilePath outputApk)
		{
			ProcessArgumentBuilder builder = new ProcessArgumentBuilder()
				.Append("-f")
				.Append("-v")
				.Append("4")
				.AppendQuoted(inputApk.MakeAbsolute(_context.Environment).FullPath)
				.AppendQuoted(outputApk.MakeAbsolute(_context.Environment).FullPath);
			
			IProcess process = RunProcess(new ToolSettings(), builder);
			process.WaitForExit();
			return (process.GetExitCode() == 0);
		}
	}
}