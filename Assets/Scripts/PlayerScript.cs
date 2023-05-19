using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerScript : MonoBehaviour
{
    [SerializeField] MapManagerScript mapMan;
    [SerializeField] int StepLen;
    [SerializeField] public int stepsTaken;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        bool moved=false;
        if(Input.GetKeyDown(KeyCode.W))
        {
            gameObject.transform.position += nextStep(Vector3.forward*StepLen);
            stepsTaken++;
            moved = true;
        }
        else if(Input.GetKeyDown(KeyCode.A))
        {
            gameObject.transform.position += nextStep(Vector3.left*StepLen);
            stepsTaken++;
            moved = true;
        }
        else if(Input.GetKeyDown(KeyCode.S))
        {
            gameObject.transform.position += nextStep(Vector3.back*StepLen);
            stepsTaken++;
            moved = true;
        }
        else if(Input.GetKeyDown(KeyCode.D))
        {
            gameObject.transform.position += nextStep(Vector3.right*StepLen);
            stepsTaken++;
            moved = true;
        }
        if (moved)
        {
            mapMan.UpdatePlane(mapMan.RoundVector3(gameObject.transform.position));
        }
    }
    Vector3 nextStep(Vector3 desiredPos)
    {
        return (mapMan.CheckCellifEmpty(transform.position+desiredPos))?desiredPos:Vector3.zero;
    }
}
