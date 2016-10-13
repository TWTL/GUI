using UnityEngine;
using System.Collections;

/// <summary>
/// Base class for all singleton implementations
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class BaseSingleton<T> : MonoBehaviour
	where T : BaseSingleton<T>
{
	// Members

	public static T instance { get; private set; }

	void Awake()
	{
		instance    = this as T;

		Initialize();
	}

	/// <summary>
	/// Initialize method stub. ( replacing Awake() )
	/// </summary>
	protected virtual void Initialize()
	{
		//
	}
}
