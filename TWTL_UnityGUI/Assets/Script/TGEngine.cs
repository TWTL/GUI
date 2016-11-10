using UnityEngine;
using System.Collections;

public class TGEngine : MonoBehaviour
{
	void Start()
	{
		// Initialize process


		// Protocol Startup

		StartCoroutine(co_LateStart());
	}

	IEnumerator co_LateStart()
	{
		yield return null;
		var comModule   = TGComModule.instance;
		comModule.StartRequestConnection(ReqConnectionCallback);
	}

	void ReqConnectionCallback(TGComModule.Status status)
	{
		if (status != TGComModule.Status.RequestChannelOpen)
			return;

		var comModule   = TGComModule.instance;
		comModule.StartTrapConnection(TrapConnectionCallback);

		// let the engine know the trap port number
		// TODO
	}

	void TrapConnectionCallback(TGComModule.Status status)
	{
		// we're all done, so......
	}
}
