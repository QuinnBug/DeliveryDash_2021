using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Event_Manager : Singleton<Event_Manager>
{
    UnityEvent _OnGameStart = new UnityEvent();
}
