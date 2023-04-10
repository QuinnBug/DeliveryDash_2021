using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Event_Manager : Singleton<Event_Manager>
{
    public UnityEvent _OnGameStart = new UnityEvent();
    public UnityEvent _OnTerrainGenerated = new UnityEvent();
    public UnityEvent _OnRoadsGenerated = new UnityEvent();

    public void Start()
    {
        StartCoroutine(DelayedStart());
    }

    private IEnumerator DelayedStart() 
    {
        yield return new WaitForSeconds(0.1f);
        _OnGameStart.Invoke();
    }
}

public class BoolEvent : UnityEvent<bool> { }
