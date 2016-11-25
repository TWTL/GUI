using UnityEngine;
using System.Collections;

public static class TGUtils
{
	
}

namespace TGExtensions
{
	public static class TGExtension
	{
		/// <summary>
		/// Starts a coroutine that is executed each frame and recieves time ratio of (elapsed time) / (duration)
		/// </summary>
		/// <param name="self"></param>
		/// <param name="duration"></param>
		/// <param name="func"></param>
		/// <returns></returns>
		public static Coroutine StartTimedCoroutine(this MonoBehaviour self, float duration, System.Action<float> func)
		{
			return self.StartCoroutine(co_TimeCoroutine(duration, func));
		}

		static IEnumerator co_TimeCoroutine(float duration, System.Action<float> func)
		{
			var startTime   = Time.time;
			var elapsed     = 0f;

			while ((elapsed = Time.time - startTime) < duration)
			{
				var t       = elapsed / duration;
				func(t);
				yield return null;
			}
			func(1);
		}
	}
}
