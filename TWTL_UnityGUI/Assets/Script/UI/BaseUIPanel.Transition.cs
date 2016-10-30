using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public abstract partial class BaseUIPanel : MonoBehaviour
{
	public interface IUITransition
	{
		void Start();
		void Stop();

		bool isFinished { get; }
		float duration { get; }

		event System.Action finished;
		event System.Action interrupted;
	}

	public abstract class BaseUITransition<TargetT> : IUITransition
		where TargetT : BaseUIPanel
	{
		// Members

		Coroutine       m_coroutine;

		protected TargetT target { get; private set; }
		public bool isFinished { get; private set; }
		public float duration { get; set; }

		public event System.Action finished;
		public event System.Action interrupted;


		public BaseUITransition(TargetT target)
		{
			this.target = target;
		}

		public void Start()
		{
			isFinished  = false;
			Stop();
			m_coroutine = target.StartCoroutine(co_Loop(duration));
		}

		IEnumerator co_Loop(float duration)
		{
			OnStart();

			var startTime   = Time.time;
			var elapsed     = 0f;
			while ((elapsed = Time.time - startTime) < duration)
			{
				OnEachLoop(elapsed / duration);
				yield return null;
			}
			OnEachLoop(1);

			isFinished  = true;
			OnFinish();
			if (finished != null)
				finished();
		}

		public void Stop()
		{
			if (m_coroutine != null)
			{
				target.StopCoroutine(m_coroutine);
				m_coroutine = null;
				if (interrupted != null)
					interrupted();
			}
		}


		protected virtual void OnStart() { }
		protected virtual void OnEachLoop(float timeRatio) { }
		protected virtual void OnFinish() { }
	}

	/// <summary>
	/// transition animation types
	/// </summary>
	protected enum TransitionType
	{
		Open,
		Close,
		GetBusy,
		BackToIdle,
	}


	protected class TransitionController
	{
		// Members

		Dictionary<TransitionType, IUITransition>   m_transitionDict  = new Dictionary<TransitionType, IUITransition>();
		IUITransition                               m_curTrans;


		public IUITransition this[TransitionType type]
		{
			set { Setup(type, value); }
			get { return m_transitionDict[type]; }
		}

		public void Setup(TransitionType type, IUITransition transition)
		{
			m_transitionDict[type]  = transition;
		}

		public void DoTransition(TransitionType type)
		{
			IUITransition newTrans;
			m_transitionDict.TryGetValue(type, out newTrans);
			if (newTrans != null)                                   // if new transition is available...
			{
				if (m_curTrans != null && !m_curTrans.isFinished)   // stop previous transition, is not finished
				{
					m_curTrans.Stop();
				}

				m_curTrans  = newTrans;
				newTrans.Start();
			}
		}
	}

	//================================================================

	// Default Transitions

	public class FadeIn : BaseUITransition<BaseUIPanel>
	{
		public FadeIn(BaseUIPanel target) : base(target) { }

		float       m_startAlpha;

		protected override void OnStart()
		{
			m_startAlpha    = target.alpha;
		}

		protected override void OnEachLoop(float timeRatio)
		{
			target.alpha    = Mathf.Lerp(m_startAlpha, 1, timeRatio);
		}
	}

	public class FadeOut : BaseUITransition<BaseUIPanel>
	{
		public FadeOut(BaseUIPanel target) : base(target) { }

		float       m_startAlpha;

		protected override void OnStart()
		{
			m_startAlpha    = target.alpha;
		}

		protected override void OnEachLoop(float timeRatio)
		{
			target.alpha    = Mathf.Lerp(m_startAlpha, 0, timeRatio);
		}
	}
}
