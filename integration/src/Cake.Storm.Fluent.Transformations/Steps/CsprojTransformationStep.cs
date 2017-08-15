﻿using Cake.Storm.Fluent.Common;
using Cake.Storm.Fluent.Interfaces;
using Cake.Storm.Fluent.InternalExtensions;
using Cake.Storm.Fluent.Steps;
using Cake.Storm.Fluent.Transformations.Interfaces;

namespace Cake.Storm.Fluent.Transformations.Steps
{
	[PreBuildStep]
	internal class CsprojTransformationStep : IStep
	{
		private readonly string _projectFile;
		private readonly ICsprojTransformationAction _transformation;

		public CsprojTransformationStep(string projectFile, ICsprojTransformationAction transformation)
		{
			_projectFile = projectFile;
			_transformation = transformation;
		}

		public void Execute(IConfiguration configuration)
		{
			string projectFile = _projectFile ?? configuration.GetSimple<string>(ConfigurationConstants.PROJECT_KEY);
			
			configuration.FileExistsOrThrow(projectFile);
			
			_transformation.Execute(projectFile, configuration);
		}
	}
}