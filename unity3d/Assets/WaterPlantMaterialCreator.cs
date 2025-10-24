// using System;
// using System.Collections;
// using System.Collections.Generic;
// using System.IO;
// using System.Linq;
// using System.Text;
// using System.Threading;
// using System.Threading.Tasks;
// using UnityEngine;
// using UnityEngine.SceneManagement;
// using UnityEngine.EventSystems;
// using UnityEngine.UI;
// using UnityEditor;
// using UnityEditorInternal;
// using UnityEngine.Rendering;
// using Object = UnityEngine.Object;
// using Random = UnityEngine.Random;

// namespace CodeNamespace
// {
//     public class CodeClass
//     {
//         public static void Run()
//         {
//                     float radius = 50.0f;
//                     int numberOfCubes = 100;
//                     GameObject centerObject = GameObject.Find(DysonSphereCenter);

//                     if (centerObject == null)
//                     {
//                         Debug.LogError(DysonSphereCenter object not found. Please create it first.);
//                         return;
//                     }

//                     for (int i = 0; i < numberOfCubes; i++)
//                     {
//                         Vector3 randomPosition = Random.onUnitSphere * radius;
//                         GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
//                         cube.name = DysonCube_ + i;
//                         cube.transform.position = centerObject.transform.position + randomPosition;
//                         cube.transform.LookAt(centerObject.transform);
//                         cube.transform.SetParent(centerObject.transform);
//                     }
//                     Debug.Log($Successfully created {numberOfCubes} cubes in a Dyson Sphere layout with radius {radius}.);
//         }
//     }
// }