using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.IO.Ports;

using System.Runtime.InteropServices;  // dllimport, Marshal

public delegate IntPtr WndProcDelegate(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);

public class UIManager : MonoBehaviour
{
    

    [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
    public static extern System.IntPtr GetForegroundWindow();

    [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
    public static extern int SetWindowLong32(HandleRef hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
    public static extern IntPtr SetWindowLongPtr64(HandleRef hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll", EntryPoint = "DefWindowProcA")]
    public static extern IntPtr DefWindowProc(IntPtr hWnd, uint wMsg, IntPtr wParam, IntPtr lParam);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    public static extern System.IntPtr GetActiveWindow();

    [DllImport("User32.dll", EntryPoint = "FindWindow")]
    public static extern IntPtr FindWindow(string className, string windowName);

    [DllImport("User32.dll", EntryPoint = "MessageBox")]
    public static extern int MessageBox(IntPtr hwnd, string text, string title, uint style);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern UInt16 RegisterWindowMessage(string lpString);


    [DllImport("PVA.dll")]
    public static extern void PVA_init();

    [DllImport("PVA.dll")]
    public static extern void PVA_term();

    [DllImport("PVA.dll")]
    public static extern int PVA_getCamraStatus();

    [DllImport("PVA.dll")]
    public static extern int PVA_config(IntPtr hwnd);

    [DllImport("PVA.dll")]
    public static extern int PVA_startDetect(IntPtr hwnd, uint uInt);

    [DllImport("PVA.dll")]
    public static extern int PVA_endDetect(IntPtr hwnd);

    [DllImport("PVA.dll")]
    public static extern int PVA_startBall();

    [DllImport("PVA.dll")]
    public static extern int PVA_endBall();

    private IntPtr hWnd;    // This.Handle

    /////////////////////////////////////////////////////////
    // IO  신호
    private const string I_FOOT_PRESS = "C";    // 발판 누름
    private const string I_BALL_EMPTY = "D";    // 공 없음

    private const string O_MOTOR_ON = "Y";      // 피칭기 모터 On
    private const string O_MOTOR_OFF = "I";     // 피칭기 모터 Off

    private const string O_PUSH_ON = "T";       // Push On
    private const string O_PUSH_OFF = "U";      // Push Off

    private const string O_LED_ON = "N";        // LED On
    private const string O_LED_OFF = "M";       // LED Off

    /////////////////////////////////////////////////////////
    // Serial 관련
    private SerialPort m_Port;
    //public event dataReceived DataReceived;
    //private SerialPort serialPort = new SerialPort();
    private string portName = "COM1";
    private int baudRate = 19200;
    private int dataBits = 8;
    private StopBits stopBits = StopBits.One;
    private Parity parity = Parity.None;
    private Handshake handshake = Handshake.None;
    //private string tString = string.Empty;
    //private byte terminator = 0x4;
    private int readBufferSize = 4096;
    private int readTimeout = 100;
    private int writeBufferSize = 4096;
    private int writeTimeout = -1;


    /////////////////////////////////////////////////////////
    // Message ID Define
    const Int32 WM_USER = 0x400;
    const Int32 WM_COPYDATA = 0x04A;
    const Int32 WM_NCDESTROY = 0x082;
    const Int32 WM_WINDOWPOSCHANGING = 0x046;

    private int g_camera_status;
    private int g_detect_start;
    private uint g_uDetectNotify;

    private HandleRef hMainWindow;
    private IntPtr oldWndProcPtr;
    private IntPtr newWndProcPtr;
    private WndProcDelegate newWndProc;

    private Text textLog;










    // Use this for initialization
    void Start () {

        hWnd = GetWindowHandle();

        textLog = GameObject.Find("txtDisp").GetComponent<Text>();

        textLog.text = "";

        Debug.Log("Window Handle=" + hWnd.ToString());
        txtDisplay("Window Handle=" + hWnd.ToString());
        MessageBox("Window Handle", "Handle=" + hWnd.ToString());

        //Init();
    }

    void Update()
    {
        //print ("Serial Update");

        
    }



    void OnApplicationQuit()
    {
        //m_RunThreadSerial = false;

        if (m_Port != null)
        {
            if (m_Port.IsOpen)
            {
                //print("closing serial port");

                Debug.Log("closing serial port");
                txtDisplay("closing serial port");
                MessageBox("closing serial port", "Handle=" + hWnd.ToString());
                m_Port.Close();
            }

            m_Port = null;
        }

        //Stop();
    }

    public void txtDisplay(string txt)
    {
        //        textLog.text = txt + "\r\n" + textLog.text;
        textLog.text = txt;
    }

    public void MessageBox(string title, string msg)
    {
        MessageBox(hWnd, msg, title, 0);
    }

    public static System.IntPtr GetWindowHandle()
    {
        return GetActiveWindow();
    }

    public void Init()
    {
        //////////////////////////////
        // PVA Message 처리
        // Window 프로시저 후킹
        hMainWindow = new HandleRef(null, GetActiveWindow());
        newWndProc = new WndProcDelegate(wndProc);
        newWndProcPtr = Marshal.GetFunctionPointerForDelegate(newWndProc);
        oldWndProcPtr = SetWindowLongPtr(hMainWindow, -4, newWndProcPtr);

        //g_camera_status = PVAResult_Uninitialize;
        g_detect_start = 0;
        g_uDetectNotify = 0;

        // System Message ID
        g_uDetectNotify = RegisterWindowMessage("DetectNotify");  // Message 수신 정의

        PVA_init();
        PVA_startDetect(hWnd, g_uDetectNotify);

        PVA_init();
    }

    /// <summary>
    /// Process 중지 : Serial Port 및 관련 Thread 중지/Close, PVA 종료)
    /// </summary>
    public void Stop()
    {
        //m_RunThreadSerial = false;

        //thread_Serial.Abort();

        //Port_Close();    // Serial Port Close




        //SetWindowLongPtr(hMainWindow, -4, oldWndProcPtr);
        //hMainWindow = new HandleRef(null, IntPtr.Zero);
        //oldWndProcPtr = IntPtr.Zero;
        //newWndProcPtr = IntPtr.Zero;
        //newWndProc = null;

        //PVA_endBall();
        //PVA_endDetect(hWnd);
        //PVA_term();

        //Stop_pva();
    }

    void OnGUI()
    {
        // 창 핸들이 바뀐 것으로 초기화
        if (hMainWindow.Handle == IntPtr.Zero)
        {
            //Start_pva();
        }
    }

    

    // P/Invoke정의 (pinvoke.net참조)
    public static IntPtr SetWindowLongPtr(HandleRef hwnd, int nIndex, IntPtr dwNewLong)
    {
        if (IntPtr.Size == 8)
            return SetWindowLongPtr64(hwnd, nIndex, dwNewLong);
        else
        {
            return new IntPtr(SetWindowLong32(hwnd, nIndex, dwNewLong.ToInt32()));
        }
    }

    private IntPtr wndProc(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == WM_NCDESTROY || msg == WM_WINDOWPOSCHANGING)
        {
            //Start();

            //return (IntPtr)0;
            //return DefWindowProc(hwnd, msg, wParam, lParam);
        }
        else
        {
            if (msg == g_uDetectNotify)
            {
            }
        }

        return DefWindowProc(hwnd, msg, wParam, lParam);
    }

    //////////////////////////////////////////////////////////
    // 각종 버튼 처리
    public void OnClickBtn_StartBall()
    {
        Debug.Log("Press StartBall Button");

        //PVA_init();
        //PVA_startDetect(hWnd, g_uDetectNotify);

        txtDisplay("DetectNotify Start");

        //PVA_startBall();
        txtDisplay("Start Ball");
    }

    public void OnClickBtn_EndBall()
    {
        Debug.Log("Press EndBall Button");

        //PVA_endBall();
        txtDisplay("End Ball");

        //PVA_endDetect(hWnd);
        txtDisplay("DetectNotify Stop");
    }

    public void OnClickBtn_Exit()
    {
        Stop();

        Debug.Log("Press Close Button");

        Application.Quit();
    }
}
