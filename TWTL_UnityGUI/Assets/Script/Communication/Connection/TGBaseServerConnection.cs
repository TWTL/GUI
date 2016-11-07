using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public abstract class TGBaseServerConnection<InfoT> : TGBaseConnection<InfoT>
	where InfoT : TGBaseConnection<InfoT>.IConnectionInfo
{
	
}
