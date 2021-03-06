﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Cake.Core;
using Cake.Core.IO;
using Cake.Storm.Fluent.Common;
using Cake.Storm.Fluent.iOS.Common;
using Cake.Storm.Fluent.iOS.Interfaces;
using Cake.Storm.Fluent.Interfaces;
using Cake.Storm.Fluent.InternalExtensions;

namespace Cake.Storm.Fluent.iOS.Models
{
	internal class PListTransformation : IPListTransformationAction
	{
		private readonly IFluentContext _configurationContext;
		private const string PARAMETER_KEY = "$PARAMETER$";

		private const string BUNDLE_KEY = "CFBundleIdentifier";
		private const string VERSION_KEY = "CFBundleShortVersionString";
		private const string BUILD_VERSION_KEY = "CFBundleVersion";

		private const string DICT_URL_SCHEMES = "CFBundleURLTypes";
		private const string URL_SCHEME = "CFBundleURLSchemes";
		private const string URL_SCHEME_NAME = "CFBundleURLName";

		private const string BUNDLE_DISPLAY_NAME = "CFBundleDisplayName";
		private const string DISPLAY_NAME = "CFBundleName";

		//default to be used from parameter informations
		private string _version = PARAMETER_KEY;

		private string _bundleId = PARAMETER_KEY;

		private bool _isBundleNameSet;
		private string _bundleName;

		private readonly Dictionary<string, string> _urlSchemes = new Dictionary<string, string>();

		public PListTransformation(IFluentContext configurationContext)
		{
			_configurationContext = configurationContext;
		}

		public IPListTransformation WithVersion(string version)
		{
			_version = version;
			return this;
		}

		public IPListTransformation WithVersionFromParameter()
		{
			_version = PARAMETER_KEY;
			return this;
		}

		public IPListTransformation WithBundleId(string bundleId)
		{
			_bundleId = bundleId;
			return this;
		}

		public IPListTransformation WithBundleIdFromParameter()
		{
			_bundleId = PARAMETER_KEY;
			return this;
		}

		public IPListTransformation WithUrlSchemes(string name, string urlScheme)
		{
			_urlSchemes[name] = urlScheme;
			return this;
		}

		public IPListTransformation WithBundleName(string name)
		{
			if (string.IsNullOrWhiteSpace(name))
			{
				_configurationContext.CakeContext.LogAndThrow("Invalid empty bundle name for iOS PlistTransformation");
			}

			_bundleName = name;
			_isBundleNameSet = true;
			return this;
		}

		public void Execute(FilePath filePath, IConfiguration configuration)
		{
			XDocument document;
			using (Stream inputStream = configuration.Context.CakeContext.FileSystem.GetFile(filePath).OpenRead())
			{
				document = XDocument.Load(inputStream);
			}

			string bundleId = _bundleId == PARAMETER_KEY ? configuration.GetSimple<string>(iOSConstants.IOS_BUNDLE_ID_KEY) : _bundleId;
			if (string.IsNullOrEmpty(bundleId))
			{
				configuration.Context.CakeContext.LogAndThrow("Missing bundleId for iOS PlistTransformation");
				throw new Exception();
			}

			GetValueElementForKey(document, BUNDLE_KEY)?.SetValue(bundleId);

			string versionString = _version == PARAMETER_KEY ? configuration.GetSimple<string>(ConfigurationConstants.VERSION_KEY) : _version;
			if (string.IsNullOrEmpty(versionString) || !Version.TryParse(versionString, out Version version))
			{
				configuration.Context.CakeContext.LogAndThrow($"Invalid version for iOS {versionString}");
				throw new Exception();
			}

			GetValueElementForKey(document, VERSION_KEY)?.SetValue($"{version.Major}.{version.Minor}");
			GetValueElementForKey(document, BUILD_VERSION_KEY)?.SetValue($"{version.Major}.{version.Minor}.{(version.Build > 0 ? version.Build : 0)}");

			UpdateUrlSchemes(configuration, document);

			if (_isBundleNameSet)
			{
				GetValueElementForKey(document, BUNDLE_DISPLAY_NAME)?.SetValue(_bundleName);
				GetValueElementForKey(document, DISPLAY_NAME)?.SetValue(_bundleName);
			}

			using (Stream outputStream = configuration.Context.CakeContext.FileSystem.GetFile(filePath).OpenWrite())
			{
				document.Save(outputStream);
			}
		}

		private void UpdateUrlSchemes(IConfiguration configuration, XDocument document)
		{
			if (_urlSchemes.Count == 0)
			{
				return;
			}

			XElement arrayScheme = GetValueElementForKey(document, DICT_URL_SCHEMES);
			if (arrayScheme == null)
			{
				configuration.Context.CakeContext.LogAndThrow("Missing UrlSchemes array");
				throw new Exception();
			}

			foreach (KeyValuePair<string, string> keyValuePair in _urlSchemes)
			{
				string name = keyValuePair.Key;
				string scheme = keyValuePair.Value;

				FindUrlSchemeElementForName(arrayScheme, name)?.SetValue(scheme);
			}
		}

		private static XElement FindUrlSchemeElementForName(XContainer array, string name)
		{
			foreach (XElement dict in array.Elements())
			{
				List<XElement> elements = dict.Elements().ToList();
				int nameIndex = elements.FindIndex(item => item.Name.LocalName == "key" && item.Value == URL_SCHEME_NAME);
				if (nameIndex == -1)
				{
					continue;
				}

				nameIndex += 1;

				if (elements[nameIndex].Value == name)
				{
					int urlSchemeIndex = elements.FindIndex(item => item.Name.LocalName == "key" && item.Value == URL_SCHEME) + 1;
					return elements[urlSchemeIndex].Elements().First();
				}
			}

			return null;
		}

		private static XElement GetValueElementForKey(XDocument document, string key)
		{
			XElement dict = document.Root?.Elements().FirstOrDefault();
			if (dict == null)
			{
				return null;
			}

			List<XElement> values = dict.Elements().ToList();
			for (int i = 0; i < values.Count; ++i)
			{
				XElement item = values[i];
				if (item.Name.LocalName == "key" && item.Value == key)
				{
					return values[i + 1];
				}
			}

			return null;
		}
	}
}
