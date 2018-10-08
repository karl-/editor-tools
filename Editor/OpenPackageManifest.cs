using UnityEditor;

namespace Unity.Karl.Editor
{
	static class OpenPackageManifest
	{
		[MenuItem("Assets/Debug/Open Package Manifest", false)]
		static void MenuOpenPackageManifest()
		{
			SublimeEditor.Open("Packages/manifest.json");
		}
	}
}
