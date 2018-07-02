using UnityEngine;
using UnityEditor;

namespace Karl.Editor
{
	[InitializeOnLoad]
	static class DisableAutoLightmapping
	{
		static DisableAutoLightmapping()
		{
			if (Lightmapping.giWorkflowMode != Lightmapping.GIWorkflowMode.Legacy)
				Lightmapping.giWorkflowMode = Lightmapping.GIWorkflowMode.OnDemand;
		}
	}
}
