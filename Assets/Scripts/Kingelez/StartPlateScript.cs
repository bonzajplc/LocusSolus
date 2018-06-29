using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StartPlateScript : MonoBehaviour {

    Kingelez_SceneScript sceneScript_ = null;

    Vector3 origScale = Vector3.one;

    Kingelez_SceneScript Kingelez_SceneScript()
    {
        if (sceneScript_ == null)
            sceneScript_ = FindObjectOfType<Kingelez_SceneScript>();

        return sceneScript_;
    }

    // Use this for initialization
    void Start ()
    {
        origScale = transform.localScale;

        //inject process hit method
        List<LookAtObject> laoList = new List<LookAtObject>();
        LookAtObject lao = GetComponent<LookAtObject>();

        lao._processObjectMethodToCall = ProcessObjectAfterFade;
    }

    public void InitScale()
    {
        transform.localScale = origScale;
    }

    private void OnDestroy()
    {
    }

    public void ProcessObjectAfterFade(GameObject parentCanvas)
    {
        if (parentCanvas)
        {
            Canvas canvas = parentCanvas.GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.enabled = false;
            }
        }

        transform.localScale = Vector3.zero;
        gameObject.SetActive(false);

        Kingelez_SceneScript().TeleportCharacterControllerToKingelezCity();
    }

    // Update is called once per frame
    void Update ()
    {
	}
}
