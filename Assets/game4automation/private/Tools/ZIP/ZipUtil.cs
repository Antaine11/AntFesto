﻿using Ionic.Zip;
using System.IO;

namespace NaughtyAttributes
{
	public static class ZipUtil
	{

		public static void Unzip(string zipFilePath, string location)
		{

			Directory.CreateDirectory(location);

			using (Ionic.Zip.ZipFile zip = ZipFile.Read(zipFilePath))
			{

				zip.ExtractAll(location, ExtractExistingFileAction.OverwriteSilently);
			}

		}

	}
}
