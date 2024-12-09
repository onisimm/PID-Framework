﻿//----------------------------------------------------------------------------
//  Copyright (C) 2004-2012 by EMGU. All rights reserved.       
//----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.GPU;

namespace FaceDetection
{
   public static class DetectFace
   {
      public static void DetectAndDraw(Image<Bgr, Byte> image, out long detectionTime)
      {
         Stopwatch watch;
         String faceFileName = "haarcascade_frontalface_default.xml";
         String eyeFileName = "haarcascade_eye.xml";

         if (GpuInvoke.HasCuda)
         {
            using (GpuCascadeClassifier face = new GpuCascadeClassifier(faceFileName))
            using (GpuCascadeClassifier eye = new GpuCascadeClassifier(eyeFileName))
            {
               watch = Stopwatch.StartNew();
               using (GpuImage<Bgr, Byte> gpuImage = new GpuImage<Bgr, byte>(image))
               using (GpuImage<Gray, Byte> gpuGray = gpuImage.Convert<Gray, Byte>())
               {
                  Rectangle[] faceRegion = face.DetectMultiScale(gpuGray, 1.1, 10, Size.Empty);
                  foreach (Rectangle f in faceRegion)
                  {
                     //draw the face detected in the 0th (gray) channel with blue color
                     image.Draw(f, new Bgr(Color.Blue), 2);
                     using (GpuImage<Gray, Byte> faceImg = gpuGray.GetSubRect(f))
                     {
                        //For some reason a clone is required.
                        //Might be a bug of GpuCascadeClassifier in opencv
                        using (GpuImage<Gray, Byte> clone = faceImg.Clone())
                        {
                           Rectangle[] eyeRegion = eye.DetectMultiScale(clone, 1.1, 10, Size.Empty);

                           foreach (Rectangle e in eyeRegion)
                           {
                              Rectangle eyeRect = e;
                              eyeRect.Offset(f.X, f.Y);
                              image.Draw(eyeRect, new Bgr(Color.Red), 2);
                           }
                        }
                     }
                  }
               }
               watch.Stop();
            }
         }
         else
         {
            //Read the HaarCascade objects
            using (HaarCascade face = new HaarCascade(faceFileName))
            using (HaarCascade eye = new HaarCascade(eyeFileName))
            {
               watch = Stopwatch.StartNew();
               using (Image<Gray, Byte> gray = image.Convert<Gray, Byte>()) //Convert it to Grayscale
               {
                  //normalizes brightness and increases contrast of the image
                  gray._EqualizeHist();

                  //Detect the faces  from the gray scale image and store the locations as rectangle
                  //The first dimensional is the channel
                  //The second dimension is the index of the rectangle in the specific channel
                  MCvAvgComp[] facesDetected = face.Detect(
                     gray,
                     1.1,
                     10,
                     Emgu.CV.CvEnum.HAAR_DETECTION_TYPE.DO_CANNY_PRUNING,
                     new Size(20, 20),
                     Size.Empty);

                  foreach (MCvAvgComp f in facesDetected)
                  {
                     //draw the face detected in the 0th (gray) channel with blue color
                     image.Draw(f.rect, new Bgr(Color.Blue), 2);

                     //Set the region of interest on the faces
                     gray.ROI = f.rect;
                     MCvAvgComp[] eyesDetected = eye.Detect(
                        gray,
                        1.1,
                        10,
                        Emgu.CV.CvEnum.HAAR_DETECTION_TYPE.DO_CANNY_PRUNING,
                        new Size(20, 20),
                        Size.Empty);
                     gray.ROI = Rectangle.Empty;

                     foreach (MCvAvgComp e in eyesDetected)
                     {
                        Rectangle eyeRect = e.rect;
                        eyeRect.Offset(f.rect.X, f.rect.Y);
                        image.Draw(eyeRect, new Bgr(Color.Red), 2);
                     }
                  }
               }
               watch.Stop();
            }
         }
         detectionTime = watch.ElapsedMilliseconds;
      }
   }
}
