// dllmain.cpp : Definiert den Einstiegspunkt für die DLL-Anwendung.
#include <d3d11.h>
#include <IUnityInterface.h>
#include <IUnityGraphics.h>
#include <IUnityGraphicsD3D11.h> // Warum genau brauche ich das doch gleich?
#include <nvapi.h>
#include <vector>
#include <SetupAPI.h> // warum brauche ich das hier?
#include <string>

// Damit es im Plugin später keine Dopplungen der Funktionnamen gibt, die Funktionen sind weiter unten zu finden.
extern "C" {

	void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UnityPluginLoad(IUnityInterfaces* unityInterfaces);
	void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UnityPluginUnload();
	// VRS 
	bool UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API VrsSupported();
	bool UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API InitFoveation();
	
	void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UpdateGazeData();

	// Testing 
	int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API Test();
}

// Interne Funktionen
static void UNITY_INTERFACE_API OnGraphicsDeviceEvent(UnityGfxDeviceEventType eventype);
static bool UNITY_INTERFACE_API InitVrsHelper();
static bool UNITY_INTERFACE_API EnableVrsHelpers();

// Variablen 
// g_ für global Variablen
static IUnityInterfaces* g_unityInterfaces = nullptr;
static IUnityGraphics* g_graphics = nullptr;
static ID3D11Device* g_device = nullptr;
static ID3DNvVRSHelper* g_vrsHelper = nullptr;

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
// Test ob VRS verfügbar ist.
bool UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API VrsSupported() {
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
bool UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API InitFoveation() {
	if (!InitVrsHelper()) {
		return false;
	}
	if (!EnableVrsHelpers()) {
		return false;
	}
	return true;
}
// Just a testing function
int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API Test() {
	return 6;
}

// Implementierung der internen Funktionen

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

static bool InitVrsHelper() {
	NV_VRS_HELPER_INIT_PARAMS vrsHelperInitParams = {};
	vrsHelperInitParams.version = NV_VRS_HELPER_INIT_PARAMS_VER;
	vrsHelperInitParams.ppVRSHelper = &g_vrsHelper;
	NvAPI_Status NvSTatus = NvAPI_D3D_InitializeVRSHelper(g_device, &vrsHelperInitParams);

	if (NvSTatus == NVAPI_OK) {
		return true;
	}
	else {
		return false;
	}
}
static bool EnableVrsHelpers() {
	ID3D11DeviceContext* ctx = {};

	NV_VRS_HELPER_ENABLE_PARAMS enableParams;

	enableParams.version = NV_VRS_HELPER_ENABLE_PARAMS_VER;
	enableParams.ContentType = NV_VRS_CONTENT_TYPE_FOVEATED_RENDERING;
	enableParams.RenderMode = NV_VRS_RENDER_MODE_MONO;

	NvAPI_Status NvStatus = g_vrsHelper->Enable(ctx, &enableParams);
	if (NvStatus == NVAPI_OK) {
		return true;
	}
	else {
		return false;
	}
}

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
