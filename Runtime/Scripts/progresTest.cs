using System.Collections;
using System.Collections.Generic;
using Alteracia.Screenplay;
using UnityEngine;

public class progresTest : MonoBehaviour
{
    public SceneGroupWithPreAndPostActions group;
    
    // Start is called before the first frame update
    void Start()
    {
        ((ISceneActionGroup) group).OnLoadingProgress += f => Debug.Log("progress " + f);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
