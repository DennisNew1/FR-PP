// dllmain.cpp : Definiert den Einstiegspunkt für die DLL-Anwendung.
#include <d3d11.h>
#include <IUnityInterface.h>
#include <IUnityGraphics.h>
#include <IUnityGraphicsD3D11.h> // Warum genau brauche ich das doch gleich?
#include <nvapi.h>
#include <vector>
#include <SetupAPI.h> // warum brauche ich das hier?
#include <string>

// using namespace std;

// Damit es im Plugin später keine Dopplungen der Funktionnamen gibt, die Funktionen sind weiter unten zu finden.
extern "C" {
	void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UnityPluginLoad(IUnityInterfaces* unityInterfaces);
	void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UnityPluginUnload();
	bool UNITY_INTERFACE_EXPORT VrsSupported();
	int UNITY_INTERFACE_EXPORT test();
}

// Interne Funktionen
void UNITY_INTERFACE_API OnGraphicsDeviceEvent(UnityGfxDeviceEventType eventype);

// Variablen 
// g_ für global Variablen
static IUnityInterfaces* g_unityInterfaces = nullptr;
static IUnityGraphics* g_graphics = nullptr;
static ID3D11Device* g_device = nullptr;

// Implementierung der externen Funktionen
void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UnityPluginLoad(IUnityInterfaces* unityInterfaces) {
	g_unityInterfaces = unityInterfaces;
	g_graphics = g_unityInterfaces->Get<IUnityGraphics>();

	g_graphics->RegisterDeviceEventCallback(OnGraphicsDeviceEvent);
	
	OnGraphicsDeviceEvent(kUnityGfxDeviceEventInitialize);
}

void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UnityPluginUnload() {
	g_graphics->UnregisterDeviceEventCallback(OnGraphicsDeviceEvent);

	g_unityInterfaces = nullptr;
	g_graphics = nullptr;
	g_device = nullptr;
}
// Laut Unity Doku ist das der Way-to-go um an das Device zu kommen.
void UNITY_INTERFACE_API OnGraphicsDeviceEvent(UnityGfxDeviceEventType eventType) {
	
	switch (eventType)
	{
		case kUnityGfxDeviceEventInitialize:
		{
			if (g_unityInterfaces) {
				g_device = g_unityInterfaces->Get<IUnityGraphicsD3D11>()->GetDevice();
				// NvAPI initialisieren lassen
				// Funktionen geben auch NvAPI_STatus zurück evt auffangen.
				// check für NvAPI_Status::NVAPI_OK
				NvAPI_Initialize();
				NvAPI_D3D_RegisterDevice(g_device);
			}
			break;
		}
		case kUnityGfxDeviceEventShutdown:
		{
			
			NvAPI_Unload();
			break;
		}
	}
}
// Just a testing function
int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API test() {
	return 6;
}

// Test ob VRS verfügbar ist.
bool VrsSupported() {
	NV_D3D1x_GRAPHICS_CAPS caps = { 0 };

	NvAPI_Status NvStatus = NvAPI_D3D1x_GetGraphicsCapabilities(g_device, NV_D3D1x_GRAPHICS_CAPS_VER, &caps);
	if (NvStatus == NVAPI_OK) {
		// Vrs ist verfügbar
		return true;
	}
	else {
		return false;
	}
}

// Implementierung der internen Funktionen



/*
BOOL APIENTRY DllMain( HMODULE hModule,
                       DWORD  ul_reason_for_call,
                       LPVOID lpReserved
                     )
{
    switch (ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:
    case DLL_THREAD_ATTACH:
    case DLL_THREAD_DETACH:
    case DLL_PROCESS_DETACH:
        break;
    }
    return TRUE;
}
*/
