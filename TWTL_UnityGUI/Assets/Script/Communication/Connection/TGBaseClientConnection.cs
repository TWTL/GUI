using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Abstraction for "Connection" ex) socket
/// </summary>
/// <typeparam name="InfoT">connection info type</typeparam>
public abstract class TGBaseClientConnection<InfoT> : TGBaseConnection<InfoT>
	where InfoT : TGBaseConnection<InfoT>.IConnectionInfo
{
	
}
