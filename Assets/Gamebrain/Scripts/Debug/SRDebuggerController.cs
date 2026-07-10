using UnityEngine;
using System.ComponentModel;

public class SRDebuggerController
{
    
}

public partial class SROptions
{
	[Category("Level")]
	public void ComleteLevel()
	{
		UnityEngine.Debug.Log("Complete");
		GameObject.FindFirstObjectByType<GameBrain.Casual.GameManager>().TestLevelComplete();
	}

    [Category("Level")]
    public void FailLevel()
    {
        UnityEngine.Debug.Log("Fail");
        GameObject.FindFirstObjectByType<GameBrain.Casual.GameManager>().TestLevelFail();
    }

    [Category("Booster")]
    public void AddBoosters()
    {
        UnityEngine.Debug.Log("10 boosters added for each");
        GameObject.FindFirstObjectByType<GameBrain.Casual.GameManager>().TestAddBooster();
    }

    [Category("Data")]
    public void ClearGameData()
    {
        UnityEngine.Debug.Log("Game data is cleared, relaunch game");
        GameObject.FindFirstObjectByType<GameBrain.Casual.GameManager>().TestClearData();
    }

    [Category("Max Debugger")]
    public void ShowMediationDebugger()
    {
        UnityEngine.Debug.Log("ShowMediationDebugger");
#if MAX_ENABLED
        MaxSdk.ShowMediationDebugger();
#endif
    }

}
