using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;   

public class VRScreenCapture : MonoBehaviour
{
    [Header("Destination folder (Windows)")]
    public string targetFolder = @"C:\stu\praca magisterska\screenshots";

    [Header("Przycisk na kontrolerze")]
    public bool rightHand = true;   // 
    public bool usePrimary = true;  // true = A/X, false = B/Y

   // public InputAction action;     // creatin in the fly
    public InputActionReference actionRef;     // creatin in the fly

    void OnEnable()
    {
        Directory.CreateDirectory(targetFolder);

        // Np. "<XRController>{RightHand}/primaryButton"
       // var hand = rightHand ? "RightHand" : "LeftHand";
        //var btn = usePrimary ? "primaryButton" : "secondaryButton";

        //action = new InputAction(type: InputActionType.Button,
                                 //binding: $"<XRController>{{{hand}}}/{btn}");
       // action.performed += OnPerformed;
     
        actionRef.action.performed += OnPerformed;
        //action.Enable();
    }

    void OnDisable()
    {
/*        if (action != null)
        {
            action.performed -= OnPerformed;
            action.Disable();
            action.Dispose();
        }*/
    }

    void Update()
    {
        
        // Fallback: test with F12 in editor
        if (Keyboard.current != null && Keyboard.current.f12Key.wasPressedThisFrame)
            SaveShot("Keyboard F12");
    }

    private void OnPerformed(InputAction.CallbackContext _)
    {
        print("dsa");
        SaveShot("XRController button");
    }

    private void SaveShot(string source)
    {
        var name = $"screenshot_{System.DateTime.Now:yyyy-MM-dd_HH-mm-ss}.png";
        var path = Path.Combine(targetFolder, name);
        ScreenCapture.CaptureScreenshot(path,ScreenCapture.StereoScreenCaptureMode.LeftEye);
        Debug.Log($"[VRShot] Trigger: {source} → Saved: {Path.GetFullPath(path)}");
    }
}
