using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AI_Manager : Singleton<AI_Manager>
{
    public GameObject npcPrefab;
    public int npcMaxCount;
    List<AI_Base> npcs = new List<AI_Base>();

    bool active = false;
    bool canSpawn = true;

    // Start is called before the first frame update
    void Start()
    {
        Event_Manager.Instance._OnBuildingsGenerated.AddListener(StartAI);
    }

    public void Update()
    {
        if (!active) return;

        if (npcMaxCount > npcs.Count && canSpawn)
        {
            StartCoroutine(CreateNPC());
        }
        else if (npcMaxCount < npcs.Count)
        {
            AI_Base npc = npcs[npcMaxCount - 1];
            npcs.Remove(npc);
            Destroy(npc.gameObject);
        }
    }

    private IEnumerator CreateNPC()
    {
        canSpawn = false;

        GameObject unit = Instantiate(npcPrefab, transform);
        unit.name += npcs.Count;
        AI_Base unitAI = unit.GetComponent<AI_Base>();
        npcs.Add(unitAI);
        if (unitAI)
        {
            unitAI.Init();
        }
        else
        {
            Destroy(gameObject);
        }
        yield return new WaitForSeconds(0.1f);

        canSpawn = true;
    }

    // Update is called once per frame
    public void StartAI()
    {
        active = true;
        Debug.Log("Ai activated");
    }
}
