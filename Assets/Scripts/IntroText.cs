using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class IntroText : MonoBehaviour
{
    public Text _textComponent;
    private TypeWrite typeWriter;
    private WorldHandler worldHandler;
    // Start is called before the first frame update
    void Start()
    {
        // get the TypeWrite component of textcomponent's gameobject
        typeWriter = _textComponent.GetComponent<TypeWrite>();
        Debug.Log("TypeWriter: " + typeWriter);
        
        // Start the sequence coroutine
        Debug.Log("Starting intro sequence");
        StartCoroutine(ShowIntroSequence());
    }
    
    IEnumerator ShowIntroSequence()
    {
        // wait until worldHandler is ready (mapGenerated = true)
        worldHandler = GameObject.Find("WorldHandler").GetComponent<WorldHandler>();
        Debug.Log("Waiting for world generation");
        while (!worldHandler.mapGenerated)
        {
            yield return new WaitForSeconds(5f);
        }

        Debug.Log("World generated, starting intro sequence");

        typeWriter.text = "the previous meteorologist is gone.\nyou are replacing him.\nprepare your island.";
        typeWriter.TypewriteText();

        yield return new WaitForSeconds(10f);
        typeWriter.ClearText();
        yield return new WaitForSeconds(5f);

        typeWriter.text = "COLLECT ROCKS WITH LEFT CLICK.\nPLACE LEVEES WIGH RIGHT CLICK.\nLEVEES COST TWO ROCKS.\nGOOD LUCK.";
        typeWriter.TypewriteText();

        yield return new WaitForSeconds(15f);
        typeWriter.ClearText();


    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
