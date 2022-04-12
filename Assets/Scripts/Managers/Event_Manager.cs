using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Event_Manager : Singleton
{
    UnityEvent _OnGameStart = new UnityEvent();
}
