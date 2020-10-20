// dllmain.cpp : Definiert den Einstiegspunkt für die DLL-Anwendung.
#include <d3d11.h>
#include <IUnityInterface.h>
#include <IUnityGraphics.h>
#include <IUnityGraphicsD3D11.h> // Warum genau brauche ich das doch gleich?
#include <nvapi.h>
#include <SetupAPI.h> // warum brauche ich das hier?
#include <string>
#include <vector>

using namespace std;


//Structs sind etwas leichter als direkt eine ganze Mathebibliothek
struct Vec3f {
	float x;
	float y;
	float z;
};

// Damit es im Plugin später keine Dopplungen der Funktionnamen gibt, die Funktionen sind weiter unten zu finden.
extern "C" {
	// Nötig als Einstiegs und Ausstiegspunkt für Unity Plugins, sowie das einhängen von Funktionen in den Renderprozess zu beliebiger Stelle:
	void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UnityPluginLoad(IUnityInterfaces* unityInterfaces);
	void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UnityPluginUnload();
	UnityRenderingEvent UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API GetRenderEventFunc();
	// VRS 
	bool UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API VrsSupported();
	bool UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API InitFoveation();	

	// Eigene
	void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API SetGazeData(Vec3f leftEye, Vec3f rightEye);
	
	// Testing, gibt 6 zurück
	int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API Test();
}

// Interne Funktionen
static void UNITY_INTERFACE_API OnGraphicsDeviceEvent(UnityGfxDeviceEventType eventype);
static void UNITY_INTERFACE_API OnRenderEvent(int eventID);

static bool InitVrsHelper();
static bool EnableVrsHelpers();
static bool DisableVrsHelpers();
static bool InitGazeHandler();
static bool UpdateGazeData();
static bool LatchGaze();


// Variablen 
// g_ für global Variablen
static IUnityInterfaces* g_unityInterfaces = nullptr;
static IUnityGraphics* g_graphics = nullptr;
static ID3D11Device* g_device = nullptr;
static ID3DNvVRSHelper* g_vrsHelper = nullptr;
static ID3DNvGazeHandler* g_gazeHandler = nullptr;
static ID3D11DeviceContext* g_deviceContext = nullptr;

// gazeData - normalisierten Blickrichtung
static Vec3f g_leftEye = { 0.0f, 0.0f, 0.0f };
static Vec3f g_rightEye = { 0.0f, 0.0f, 0.0f };

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
	g_deviceContext = nullptr;
}
UnityRenderingEvent UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API GetRenderEventFunc() {
	return OnRenderEvent;
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
	if (!InitGazeHandler()) {
		return false;
	}
	return true;
}
void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API SetGazeData(Vec3f leftEye, Vec3f rightEye) {
	g_leftEye = leftEye;
	g_rightEye = rightEye;
}
// Nutzt normalisierte Vectoren, relativ zur Kamera.
// Just a testing function
int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API Test() {
	return 6;
}

// Implementierung der internen Funktionen

// Laut Unity Doku ist das der Way-to-go um an das Device zu kommen.
static void UNITY_INTERFACE_API OnGraphicsDeviceEvent(UnityGfxDeviceEventType eventType) {

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
static void UNITY_INTERFACE_API OnRenderEvent(int eventID) {
	/* Laut NvAPi Doku muss hier folgendes passieren:
		0 Update Gaze Data (wenn es im selben Frame passiert, was wir tun) + Latch the Gaze
		1 Enable VRS Helper
		- perform draw calls (Ich denke das macht Unity)
		3 Disable VRS Helper

	*/
	// Device Context wird öfter benötigt daher als g_ variable
	g_device->GetImmediateContext(&g_deviceContext);

	switch (eventID) {
	case 0:
		if (UpdateGazeData()) {
			LatchGaze();
		}
		break;
	case 1:
		EnableVrsHelpers();
		break;
	case 2:
		DisableVrsHelpers();		
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

	NV_VRS_HELPER_ENABLE_PARAMS enableParams = {};

	enableParams.version = NV_VRS_HELPER_ENABLE_PARAMS_VER;
	enableParams.ContentType = NV_VRS_CONTENT_TYPE_FOVEATED_RENDERING;
	enableParams.RenderMode = NV_VRS_RENDER_MODE_MONO;
	// Hier gibt es mehrere presets, man kann die Bereiche aber auch anpassen... NicetoHave & Leicht zu machen
	enableParams.sFoveatedRenderingDesc.version = NV_FOVEATED_RENDERING_DESC_VER;	
	enableParams.sFoveatedRenderingDesc.ShadingRatePreset = NV_FOVEATED_RENDERING_SHADING_RATE_PRESET_HIGHEST_PERFORMANCE;
	enableParams.sFoveatedRenderingDesc.FoveationPatternPreset = NV_FOVEATED_RENDERING_FOVEATION_PATTERN_PRESET_BALANCED;

	NvAPI_Status NvStatus = g_vrsHelper->Enable(g_deviceContext, &enableParams);
	if (NvStatus == NVAPI_OK) {
		return true;
	}
	else {
		return false;
	}
}
static bool DisableVrsHelpers() {
	NV_VRS_HELPER_DISABLE_PARAMS disableParams = {};

	disableParams.version = NV_VRS_HELPER_DISABLE_PARAMS_VER;
	NvAPI_Status NvStatus = g_vrsHelper->Disable(g_deviceContext, &disableParams);

	if (NvStatus == NVAPI_OK) {
		return true;
	}
	else {
		return false;
	}
}
static bool InitGazeHandler() {
	NV_GAZE_HANDLER_INIT_PARAMS gazeHandlerInitParams = {};
	gazeHandlerInitParams.version = NV_GAZE_HANDLER_INIT_PARAMS_VER;

	// Momentan unterstütz die API nur ein Gerät, es ist aber schon möglich mehrere IDs zu vergeben.
	gazeHandlerInitParams.GazeDataDeviceId = 0;
	gazeHandlerInitParams.GazeDataType = NV_GAZE_DATA_STEREO;
	gazeHandlerInitParams.fHorizontalFOV = 110.0f;
	gazeHandlerInitParams.fVericalFOV = 110.0f;
	gazeHandlerInitParams.ppNvGazeHandler = &g_gazeHandler;

	NvAPI_Status NvStatus = NvAPI_D3D_InitializeNvGazeHandler(g_device, &gazeHandlerInitParams);
	if (NvStatus) {
		return true;
	}
	else {
		return false;
	}	
}
static bool UpdateGazeData() {
	// Wie lange läuft die Applikation? Wie schnell wird der Timestamp hochgezählt? Weil ich das nicht weiß, nehme ich den größten Unsigned Int Typ.
	// Bis 18.446.744.073.709.551.615 sollte wohl erstmal reichen.
	static unsigned long long gazeTimestamp = 0;
	gazeTimestamp++;
	NV_FOVEATED_RENDERING_UPDATE_GAZE_DATA_PARAMS gazeDataParams = {};

	gazeDataParams.version = NV_FOVEATED_RENDERING_UPDATE_GAZE_DATA_PARAMS_VER;
	gazeDataParams.Timestamp = gazeTimestamp;

	// rightEye
	gazeDataParams.sStereoData.sRightEye.version = NV_FOVEATED_RENDERING_GAZE_DATA_PER_EYE_VER;
	gazeDataParams.sStereoData.sRightEye.fGazeDirection[0] = g_rightEye.x;
	gazeDataParams.sStereoData.sRightEye.fGazeDirection[1] = g_rightEye.y;
	gazeDataParams.sStereoData.sRightEye.fGazeDirection[2] = g_rightEye.z;
	gazeDataParams.sStereoData.sRightEye.GazeDataValidityFlags = NV_GAZE_DIRECTION_VALID;

	//leftEye
	gazeDataParams.sStereoData.sLeftEye.version = NV_FOVEATED_RENDERING_GAZE_DATA_PER_EYE_VER;
	gazeDataParams.sStereoData.sLeftEye.fGazeDirection[0] = g_leftEye.x;
	gazeDataParams.sStereoData.sLeftEye.fGazeDirection[1] = g_leftEye.y;
	gazeDataParams.sStereoData.sLeftEye.fGazeDirection[2] = g_leftEye.z;
	gazeDataParams.sStereoData.sLeftEye.GazeDataValidityFlags = NV_GAZE_DIRECTION_VALID;

	// und and den GazeHandler "reichen"
	NvAPI_Status NvStatus = g_gazeHandler->UpdateGazeData(g_deviceContext, &gazeDataParams);
	if (NvStatus == NVAPI_OK) {
		return true;
	}
	else {
		return false;
	}
}
static bool LatchGaze() {
	NV_VRS_HELPER_LATCH_GAZE_PARAMS latchGazeParams = {};
	latchGazeParams.version = NV_VRS_HELPER_ENABLE_PARAMS_VER;
	NvAPI_Status NvStatus = g_vrsHelper->LatchGaze(g_deviceContext, &latchGazeParams);

	if (NvStatus = NVAPI_OK) {
		return true;
	}
	else {
		return false;
	}
}



