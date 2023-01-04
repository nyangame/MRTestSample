using OpenCVForUnity.ArucoModule;
using OpenCVForUnity.Calib3dModule;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.ObjdetectModule;
using OpenCVForUnity.UnityUtils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using YoloV4Tiny;

public class OpenCVCtrl : MonoBehaviour
{
    [SerializeField] RawImage _image = null;
    [SerializeField] RawImage _image2 = null;
    [SerializeField] RawImage _image3 = null;
    [SerializeField] RawImage _fimage = null;
    [SerializeField] RawImage _webCam = null;
    [SerializeField] Image _write = null;
    [SerializeField] GameObject _moji = null;

    [SerializeField] YoloV4Tiny.ResourceSet _resources = null;
    [SerializeField, Range(0, 1)] float _threshold = 0.5f;

    List<Mat> straightQrcode;
    [SerializeField] string requestedDeviceName = null;
    public int requestedWidth = 640;
    public int requestedHeight = 480;

    public int requestedFPS = 30;

    WebCamTexture webCamTexture;
    WebCamDevice webCamDevice;

    Mat rgbaMat;

    QRCodeDetector detector;

    Color32[] colors;
    Texture2D texture;
    Texture2D texture2;
    Texture2D texture3;

    Mat firstSceneCaptureMat;
    Texture2D firstSceneCaptureTexture = null;
    Texture2D charucoBoardTexture;
    CharucoBoard charucoBoard;

    ObjectDetector _detector;

    bool isInitWaiting = false;
    bool hasInitDone = false;

    void Start()
    {
        detector = new QRCodeDetector();

        for (int i = 1; i < Display.displays.Length; i++)
        {
            Display.displays[i].Activate();
        }

        _detector = new ObjectDetector(_resources);

        CreateMarker();
        Initialize();
    }

    /// <summary>
    /// Initializes webcam texture.
    /// </summary>
    void Initialize()
    {
        if (isInitWaiting)
            return;

        StartCoroutine(_Initialize());
    }

    void CreateMarker()
    {
        // create dictinary.
        Dictionary dictionary = Aruco.getPredefinedDictionary(Aruco.DICT_6X6_250);

        const int borderBits = 1;
        const int chArUcoBoradMarginSize = 10;
        const int markerSize = 1000;
        const float chArUcoBoradSquareLength = 0.04f;
        const float chArUcoBoradMarkerLength = 0.02f;

        Mat markerImg = new Mat(markerSize, markerSize, CvType.CV_8UC3);
        charucoBoardTexture = new Texture2D(markerImg.cols(), markerImg.rows(), TextureFormat.RGB24, false);
        charucoBoard = CharucoBoard.create(5, 5, chArUcoBoradSquareLength, chArUcoBoradMarkerLength, dictionary);
        charucoBoard.draw(new Size(markerSize, markerSize), markerImg, chArUcoBoradMarginSize, borderBits);
        //charucoBoard.Dispose();
        Utils.matToTexture2D(markerImg, charucoBoardTexture, true, 0, true);
    }

    /// <summary>
    /// Initializes webcam texture by coroutine.
    /// </summary>
    private IEnumerator _Initialize()
    {
        if (hasInitDone)
            Dispose();

        isInitWaiting = true;

        var devices = WebCamTexture.devices;
        /*
        foreach (var device in devices)
        {
            if (device.name == null) continue;
            Debug.Log(device.name);
        }
        */
        if (!String.IsNullOrEmpty(requestedDeviceName))
        {
            int requestedDeviceIndex = -1;
            if (Int32.TryParse(requestedDeviceName, out requestedDeviceIndex))
            {
                if (requestedDeviceIndex >= 0 && requestedDeviceIndex < devices.Length)
                {
                    webCamDevice = devices[requestedDeviceIndex];
                    webCamTexture = new WebCamTexture(webCamDevice.name, requestedWidth, requestedHeight, requestedFPS);
                }
            }
            else
            {
                for (int cameraIndex = 0; cameraIndex < devices.Length; cameraIndex++)
                {
                    if (devices[cameraIndex].name == requestedDeviceName)
                    {
                        webCamDevice = devices[cameraIndex];
                        webCamTexture = new WebCamTexture(webCamDevice.name, requestedWidth, requestedHeight, requestedFPS);
                        break;
                    }
                }
            }
            if (webCamTexture == null)
                Debug.Log("Cannot find camera device " + requestedDeviceName + ".");
        }

        if (webCamTexture == null)
        {
            // Checks how many and which cameras are available on the device
            for (int cameraIndex = 0; cameraIndex < devices.Length; cameraIndex++)
            {
                if (devices[cameraIndex].kind != WebCamKind.ColorAndDepth)
                {
                    webCamDevice = devices[cameraIndex];
                    webCamTexture = new WebCamTexture(webCamDevice.name, requestedWidth, requestedHeight, requestedFPS);
                    break;
                }
            }
        }

        if (webCamTexture == null)
        {
            if (devices.Length > 0)
            {
                webCamDevice = devices[0];
                webCamTexture = new WebCamTexture(webCamDevice.name, requestedWidth, requestedHeight, requestedFPS);
            }
            else
            {
                Debug.LogError("Camera device does not exist.");
                isInitWaiting = false;
                yield break;
            }
        }

        // Starts the camera.
        webCamTexture.Play();
        _webCam.texture = webCamTexture;

        while (true)
        {
            if (webCamTexture.didUpdateThisFrame)
            {
                Debug.Log("name:" + webCamTexture.deviceName + " width:" + webCamTexture.width + " height:" + webCamTexture.height + " fps:" + webCamTexture.requestedFPS);
                Debug.Log("videoRotationAngle:" + webCamTexture.videoRotationAngle + " videoVerticallyMirrored:" + webCamTexture.videoVerticallyMirrored + " isFrongFacing:" + webCamDevice.isFrontFacing);

                isInitWaiting = false;
                hasInitDone = true;

                OnInited();

                break;
            }
            else
            {
                yield return null;
            }
        }
    }

    /// <summary>
    /// Releases all resource.
    /// </summary>
    private void Dispose()
    {
        isInitWaiting = false;
        hasInitDone = false;

        if (webCamTexture != null)
        {
            webCamTexture.Stop();
            WebCamTexture.Destroy(webCamTexture);
            webCamTexture = null;
        }
        if (rgbaMat != null)
        {
            rgbaMat.Dispose();
            rgbaMat = null;
        }
        if (texture != null)
        {
            Texture2D.Destroy(texture);
            texture = null;
        }
    }

    /// <summary>
    /// Raises the webcam texture initialized event.
    /// </summary>
    private void OnInited()
    {
        if (colors == null || colors.Length != webCamTexture.width * webCamTexture.height)
            colors = new Color32[webCamTexture.width * webCamTexture.height];
        if (texture == null || texture.width != webCamTexture.width || texture.height != webCamTexture.height)
        {
            texture = new Texture2D(webCamTexture.width, webCamTexture.height, TextureFormat.RGBA32, false);
            texture2 = new Texture2D(webCamTexture.width, webCamTexture.height, TextureFormat.RGBA32, false);
            texture3 = new Texture2D(webCamTexture.width, webCamTexture.height, TextureFormat.RGBA32, false);
        }

        rgbaMat = new Mat(webCamTexture.height, webCamTexture.width, CvType.CV_8UC4, new Scalar(0, 0, 0, 255));
        firstSceneCaptureMat = new Mat(webCamTexture.height, webCamTexture.width, CvType.CV_8UC4, new Scalar(0, 0, 0, 255));
        Utils.matToTexture2D(rgbaMat, texture, colors);

        _image.texture = charucoBoardTexture;
        _image2.texture = texture2;
        _image3.texture = texture;

        gameObject.transform.localScale = new Vector3(webCamTexture.width, webCamTexture.height, 1);
        Debug.Log("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation);

        float width = rgbaMat.width();
        float height = rgbaMat.height();

        float widthScale = (float)Screen.width / width;
        float heightScale = (float)Screen.height / height;
        if (widthScale < heightScale)
        {
            Camera.main.orthographicSize = (width * (float)Screen.height / (float)Screen.width) / 2;
        }
        else
        {
            Camera.main.orthographicSize = height / 2;
        }
    }

    class ArucoIdPos
    {
        public int Id;
        public double PosX;
        public double PosY;
    };
    Mat rgbMat;
    Mat rMat;
    bool DetectSceneMarkers()
    {
        //ARマーカーを検出する
        bool isDetect = false;
        Dictionary dictionary = Aruco.getPredefinedDictionary(Aruco.DICT_6X6_250);
        Mat ids = new Mat();
        List<Mat> corners = new List<Mat>();
        List<Mat> rejectedCorners = new List<Mat>();
        Mat recoveredIdxs = new Mat();
        DetectorParameters detectorParams = DetectorParameters.create();
        rgbMat = new Mat(webCamTexture.height, webCamTexture.width, CvType.CV_8UC3);

        float width = rgbMat.width();
        float height = rgbMat.height();

        float imageSizeScale = 1.0f;
        float widthScale = (float)Screen.width / width;
        float heightScale = (float)Screen.height / height;
        if (widthScale < heightScale)
        {
            Camera.main.orthographicSize = (width * (float)Screen.height / (float)Screen.width) / 2;
            imageSizeScale = (float)Screen.height / (float)Screen.width;
        }
        else
        {
            Camera.main.orthographicSize = height / 2;
        }

        // set camera parameters.
        int max_d = (int)Mathf.Max(width, height);
        double fx = max_d;
        double fy = max_d;
        double cx = width / 2.0f;
        double cy = height / 2.0f;
        Mat camMatrix = new Mat(3, 3, CvType.CV_64FC1);
        camMatrix.put(0, 0, fx);
        camMatrix.put(0, 1, 0);
        camMatrix.put(0, 2, cx);
        camMatrix.put(1, 0, 0);
        camMatrix.put(1, 1, fy);
        camMatrix.put(1, 2, cy);
        camMatrix.put(2, 0, 0);
        camMatrix.put(2, 1, 0);
        camMatrix.put(2, 2, 1.0f);
        //Debug.Log("camMatrix " + camMatrix.dump());

        MatOfDouble distCoeffs = new MatOfDouble(0, 0, 0, 0);
        //Debug.Log("distCoeffs " + distCoeffs.dump());


        Imgproc.cvtColor(rgbaMat, rgbMat, Imgproc.COLOR_RGBA2RGB);
        Core.flip(rgbMat, rgbMat, 0);
        //Calib3d.undistort(rgbMat, rgbMat, camMatrix, distCoeffs);

        Aruco.detectMarkers(rgbMat, dictionary, corners, ids, detectorParams, rejectedCorners);
        Aruco.refineDetectedMarkers(rgbMat, charucoBoard, corners, ids, rejectedCorners, camMatrix, distCoeffs, 10f, 3f, true, recoveredIdxs, detectorParams);

        if (ids.total() > 0)
        {
            const int charucoMinMarkers = 2;
            Mat charucoIds = new Mat();
            Mat charucoCorners = new Mat();
            const float markerLength = 0.1f;
            Mat rvec = new Mat();
            Mat tvec = new Mat();

            Aruco.interpolateCornersCharuco(corners, ids, rgbMat, charucoBoard, charucoCorners, charucoIds, camMatrix, distCoeffs, charucoMinMarkers);

            // draw markers.
            Aruco.drawDetectedMarkers(rgbMat, corners, ids, new Scalar(0, 255, 0));
            if (charucoIds.total() > 0)
            {
                Aruco.drawDetectedCornersCharuco(rgbMat, charucoCorners, charucoIds, new Scalar(0, 0, 255));
            }
            // estimate pose.
            // if at least one charuco corner detected
            if (charucoIds.total() > 0)
            {
                bool valid = Aruco.estimatePoseCharucoBoard(charucoCorners, charucoIds, charucoBoard, camMatrix, distCoeffs, rvec, tvec);

                List<ArucoIdPos> pos = new List<ArucoIdPos>();
                // if at least one board marker detected
                if (valid)
                {
                    // In this example we are processing with RGB color image, so Axis-color correspondences are X: blue, Y: green, Z: red. (Usually X: red, Y: green, Z: blue)
                    Calib3d.drawFrameAxes(rgbMat, camMatrix, distCoeffs, rvec, tvec, markerLength * 0.5f);

                    //UpdateARObjectTransform(rvec, tvec);

                    Debug.Log($"ids.total():{ids.total()} / charucoIds.total(){charucoIds.total()}");

                    for (int i = 0; i < charucoIds.total(); ++i)
                    {
                        pos.Add(new ArucoIdPos()
                        {
                            Id = (int)charucoIds.get(i, 0)[0],
                            PosX = charucoCorners.get(i, 0)[0],
                            PosY = charucoCorners.get(i, 0)[1]
                        });
                        Debug.Log($"id({i}):{String.Join(",", charucoIds.get(i, 0))}");
                        Debug.Log($"id({i}):{String.Join(",", charucoCorners.get(i, 0))}");
                        //Debug.Log($"id({i}):{charucoIds.get(i, 1).GetValue(0)}"); //, {charucoIds.get(i, 0).GetValue(2)}
                    }

                    //Idが0,1,4,5のマーカーを探して、画面の位置を特定する
                    var p0 = pos.Where(p => p.Id == 0).Single();
                    var p1 = pos.Where(p => p.Id == 1).Single();
                    var p4 = pos.Where(p => p.Id == 4).Single();
                    var p5 = pos.Where(p => p.Id == 5).Single();

                    if (p0 == null || p1 == null || p4 == null || p5 == null) return false;

                    float xSub = (float)(p1.PosX - p0.PosX);
                    float ySub = (float)(p4.PosY - p0.PosY);
                    float x1 = (float)p0.PosX - xSub;
                    float x2 = (float)p1.PosX + xSub * 3;
                    float y1 = (float)p0.PosY - ySub;
                    float y2 = (float)p4.PosY + ySub * 3;
                    float w = (float)rgbMat.width();//xSub * 5; // 
                    float h = (float)rgbMat.height();//ySub * 5; // 

                    Mat srcPointMat = new Mat(4, 2, CvType.CV_32F);
                    float[] srcPoints = new float[] { x1, y1, x2, y1, x1, y2, x2, y2 };
                    srcPointMat.put(0, 0, srcPoints);

                    Mat dstPointMat = new Mat(4, 2, CvType.CV_32F);
                    float[] dstPoints = new float[] { 0.0f, 0.0f, w, 0.0f, 0.0f, h, w, h };
                    dstPointMat.put(0, 0, dstPoints);

                    Size s = new Size();
                    s.width = w;
                    s.height = h;
                    Debug.Log($"({x1},{y1})({x2},{y2}) {rgbMat.width()}, {rgbMat.height()}, {rgbMat.size()}");
                    rMat = Imgproc.getPerspectiveTransform(srcPointMat, dstPointMat);
                    Imgproc.warpPerspective(rgbMat, firstSceneCaptureMat, rMat, s);

                    _image.texture = texture;
                    isDetect = true;
                }
            }

            firstSceneCaptureTexture = new Texture2D(webCamTexture.width, webCamTexture.height, TextureFormat.RGBA32, false);
            _fimage.texture = firstSceneCaptureTexture;

            //Imgproc.putText (rgbaMat, "W:" + rgbaMat.width () + " H:" + rgbaMat.height () + " SO:" + Screen.orientation, new Point (5, rgbaMat.rows () - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
            Imgproc.cvtColor(firstSceneCaptureMat, firstSceneCaptureMat, Imgproc.COLOR_RGB2RGBA);
            Utils.matToTexture2D(firstSceneCaptureMat, firstSceneCaptureTexture);
        }

        if (rejectedCorners.Count > 0)
        {
            Aruco.drawDetectedMarkers(rgbMat, rejectedCorners, new Mat(), new Scalar(255, 0, 0));
        }
        Utils.matToTexture2D(rgbMat, texture2);

        return isDetect;
    }

    bool DetectMarker(out Vector2 pos)
    {
        pos = new Vector2(0,0);

        //ARマーカーを検出する
        Dictionary dictionary = Aruco.getPredefinedDictionary(Aruco.DICT_4X4_50);
        Mat ids = new Mat();
        List<Mat> corners = new List<Mat>();
        List<Mat> rejectedCorners = new List<Mat>();
        Mat recoveredIdxs = new Mat();
        DetectorParameters detectorParams = DetectorParameters.create();

        //Mat _rgbaMat = new Mat(webCamTexture.height, webCamTexture.width, CvType.CV_8UC3);
        //rgbMat = new Mat(webCamTexture.height, webCamTexture.width, CvType.CV_8UC3);

        float width = rgbMat.width();
        float height = rgbMat.height();
        rgbMat = new Mat((int)height, (int)width, CvType.CV_8UC3);

        float imageSizeScale = 1.0f;
        float widthScale = (float)Screen.width / width;
        float heightScale = (float)Screen.height / height;
        if (widthScale < heightScale)
        {
            Camera.main.orthographicSize = (width * (float)Screen.height / (float)Screen.width) / 2;
            imageSizeScale = (float)Screen.height / (float)Screen.width;
        }
        else
        {
            Camera.main.orthographicSize = height / 2;
        }

        // set camera parameters.
        int max_d = (int)Mathf.Max(width, height);
        double fx = max_d;
        double fy = max_d;
        double cx = width / 2.0f;
        double cy = height / 2.0f;
        Mat camMatrix = new Mat(3, 3, CvType.CV_64FC1);
        camMatrix.put(0, 0, fx);
        camMatrix.put(0, 1, 0);
        camMatrix.put(0, 2, cx);
        camMatrix.put(1, 0, 0);
        camMatrix.put(1, 1, fy);
        camMatrix.put(1, 2, cy);
        camMatrix.put(2, 0, 0);
        camMatrix.put(2, 1, 0);
        camMatrix.put(2, 2, 1.0f);

        MatOfDouble distCoeffs = new MatOfDouble(0, 0, 0, 0);
        //Debug.Log("distCoeffs " + distCoeffs.dump());

        //Utils.webCamTextureToMat(webCamTexture, _rgbaMat, colors);
        Imgproc.cvtColor(rgbaMat, rgbMat, Imgproc.COLOR_RGBA2RGB);
        //Core.flip(rgbMat, rgbMat, 0);

        Aruco.detectMarkers(rgbMat, dictionary, corners, ids, detectorParams, rejectedCorners);

        if (ids.total() > 0)
        {
            Mat diamondIds = new Mat();
            List<Mat> diamondCorners = new List<Mat>();
            const float diamondSquareLength = 0.1f;
            const float diamondMarkerLength = 0.06f;

            // detect diamond markers.
            Aruco.detectCharucoDiamond(rgbMat, corners, ids, diamondSquareLength / diamondMarkerLength, diamondCorners, diamondIds, camMatrix, distCoeffs);

            /*
            // draw markers.
            Aruco.drawDetectedMarkers(rgbMat, corners, ids, new Scalar(0, 255, 0));

            // draw diamond markers.
            Aruco.drawDetectedDiamonds(rgbMat, diamondCorners, diamondIds, new Scalar(0, 0, 255));
            */

            // estimate pose.
            // if at least one charuco corner detected
            if (ids.total() > 0)
            {
                var id = ids.get(0, 0)[0];
                double x = corners[0].get(0, 0)[0];
                double y = corners[0].get(0, 0)[1];

                pos.x = (float)x * widthScale;
                pos.y = height - (float)y * heightScale;

                return true;
            }
        }

        if (rejectedCorners.Count > 0)
        {
            Aruco.drawDetectedMarkers(rgbMat, rejectedCorners, new Mat(), new Scalar(255, 0, 0));
        }

        Utils.matToTexture2D(rgbMat, texture2);

        return false;
    }


    [SerializeField] bool[] setString = new bool[4];
    [SerializeField] int[] setIds = new int[4];
    [SerializeField] string[] setValue = new string[4];
    void DetectMarkerAndDrawString()
    {
        //ARマーカーを検出する
        Dictionary dictionary = Aruco.getPredefinedDictionary(Aruco.DICT_4X4_50);
        Mat ids = new Mat();
        List<Mat> corners = new List<Mat>();
        List<Mat> rejectedCorners = new List<Mat>();
        Mat recoveredIdxs = new Mat();
        DetectorParameters detectorParams = DetectorParameters.create();

        //Mat _rgbaMat = new Mat(webCamTexture.height, webCamTexture.width, CvType.CV_8UC3);
        //rgbMat = new Mat(webCamTexture.height, webCamTexture.width, CvType.CV_8UC3);

        float width = rgbaMat.width();
        float height = rgbaMat.height();
        rgbMat = new Mat((int)height, (int)width, CvType.CV_8UC3);

        float imageSizeScale = 1.0f;
        float widthScale = (float)Screen.width / width;
        float heightScale = (float)Screen.height / height;
        if (widthScale < heightScale)
        {
            Camera.main.orthographicSize = (width * (float)Screen.height / (float)Screen.width) / 2;
            imageSizeScale = (float)Screen.height / (float)Screen.width;
        }
        else
        {
            Camera.main.orthographicSize = height / 2;
        }

        // set camera parameters.
        int max_d = (int)Mathf.Max(width, height);
        double fx = max_d;
        double fy = max_d;
        double cx = width / 2.0f;
        double cy = height / 2.0f;
        Mat camMatrix = new Mat(3, 3, CvType.CV_64FC1);
        camMatrix.put(0, 0, fx);
        camMatrix.put(0, 1, 0);
        camMatrix.put(0, 2, cx);
        camMatrix.put(1, 0, 0);
        camMatrix.put(1, 1, fy);
        camMatrix.put(1, 2, cy);
        camMatrix.put(2, 0, 0);
        camMatrix.put(2, 1, 0);
        camMatrix.put(2, 2, 1.0f);

        MatOfDouble distCoeffs = new MatOfDouble(0, 0, 0, 0);
        //Debug.Log("distCoeffs " + distCoeffs.dump());

        //Utils.webCamTextureToMat(webCamTexture, _rgbaMat, colors);
        Imgproc.cvtColor(rgbaMat, rgbMat, Imgproc.COLOR_RGBA2RGB);
        Core.flip(rgbMat, rgbMat, 0);

        Aruco.detectMarkers(rgbMat, dictionary, corners, ids, detectorParams, rejectedCorners);
        //detect diamond markers.

        if (ids.total() > 0)
        {
            for (int ii = 0; ii < ids.total(); ii++)
            {
                var Id = (int)ids.get(ii, 0)[0];
                int index = -1;
                for (int i = 0; i < setIds.Length; ++i)
                {
                    if (Id != setIds[i]) continue;
                    //if (setString[i]) continue;

                    index = i;
                    break;
                }

                if (index == -1) return;

                Mat rvecs = new Mat();
                Mat tvecs = new Mat();

                // estimate pose.
                Aruco.estimatePoseSingleMarkers(corners, 0.01f, camMatrix, distCoeffs, rvecs, tvecs);

                double x = corners[0].get(0, 0)[0] * widthScale;// * 1920.0;
                double y = Screen.height - corners[0].get(0, 0)[1] * heightScale;// * 1080.0;

                for (int i = 0; i < rvecs.total(); i++)
                {
                    using (Mat rvec = new Mat(rvecs, new OpenCVForUnity.CoreModule.Rect(0, i, 1, 1)))
                    using (Mat tvec = new Mat(tvecs, new OpenCVForUnity.CoreModule.Rect(0, i, 1, 1)))
                    {
                        // In this example we are processing with RGB color image, so Axis-color correspondences are X: blue, Y: green, Z: red. (Usually X: red, Y: green, Z: blue)
                        Calib3d.drawFrameAxes(rgbMat, camMatrix, distCoeffs, rvec, tvec, 0.01f * 0.5f);

                        // This example can display the ARObject on only first detected marker.
                        if (i == 0)
                        {
                            var moji = Instantiate(_moji);
                            moji.transform.parent = GameObject.Find("/Canvas").transform;
                            var text = moji.GetComponent<TMPro.TextMeshProUGUI>();
                            text.text = setValue[index];
                            setString[index] = true;
                            UpdateARObjectTransform(rvec, tvec, moji);
                            moji.transform.position = new Vector3((float)x, (float)y, 0);

                            Utils.matToTexture2D(rgbMat, texture3);
                            _fimage.texture = texture3;
                        }
                    }
                }
            }
        }

        if (rejectedCorners.Count > 0)
        {
            Aruco.drawDetectedMarkers(rgbMat, rejectedCorners, new Mat(), new Scalar(255, 0, 0));
        }

        Utils.matToTexture2D(rgbMat, texture2);
    }


    void Update()
    {
        if (hasInitDone && !webCamTexture.isPlaying)
            webCamTexture.Play();

        if (hasInitDone && webCamTexture.isPlaying && webCamTexture.didUpdateThisFrame)
        {
            Utils.webCamTextureToMat(webCamTexture, rgbaMat, colors);

            if (firstSceneCaptureTexture == null)
            {
                Utils.matToTexture2D(rgbaMat, texture, colors);
                DetectSceneMarkers();
            }
            else
            {
                Imgproc.warpPerspective(rgbaMat, rgbaMat, rMat, rgbaMat.size(), Imgproc.INTER_LINEAR);
                Utils.matToTexture2D(rgbaMat, texture, colors);

                /*
                _detector.ProcessImage(texture, _threshold);
                var i = 0;
                foreach (var d in _detector.Detections)
                {
                    Debug.Log($"({d.x}, {d.y}) - {d.classIndex} {d.score}%");

                    var img = Instantiate(_write);
                    var rt = img.GetComponent<RectTransform>();
                    rt.parent = _write.transform.parent;
                    rt.anchoredPosition = new Vector2(d.x * 1920, d.y * 1080);
                }
                */

                DetectMarkerAndDrawString();
            }
        }
    }

    /// <summary>
    /// Raises the destroy event.
    /// </summary>
    void OnDestroy()
    {
        Dispose();
    }

    /// <summary>
    /// Raises the pause button click event.
    /// </summary>
    public void OnPauseButtonClick()
    {
        if (hasInitDone)
            webCamTexture.Pause();
    }

    /// <summary>
    /// Raises the stop button click event.
    /// </summary>
    public void OnStopButtonClick()
    {
        if (hasInitDone)
            webCamTexture.Stop();
    }

    void UpdateARObjectTransform(Mat rvec, Mat tvec, GameObject obj)
    {
        // Convert to unity pose data.
        double[] rvecArr = new double[3];
        rvec.get(0, 0, rvecArr);
        double[] tvecArr = new double[3];
        tvec.get(0, 0, tvecArr);
        PoseData poseData = ARUtils.ConvertRvecTvecToPoseData(rvecArr, tvecArr);

        // Changes in pos/rot below these thresholds are ignored.
        //if (enableLowPassFilter)
        //{
        //    ARUtils.LowpassPoseData(ref oldPoseData, ref poseData, positionLowPass, rotationLowPass);
        //}
        //oldPoseData = poseData;

        // Convert to transform matrix.
        Matrix4x4 ARM;
        ARM = ARUtils.ConvertPoseDataToMatrix(ref poseData, true);
        ARM = obj.transform.localToWorldMatrix * ARM.inverse;
        ARUtils.SetTransformFromMatrix(obj.transform, ref ARM);
        obj.transform.Rotate(180,0,0);
    }
}
