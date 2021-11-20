using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameCamera : MonoBehaviour
{

	//��������Ŀ�����壬һ����һ��������
	public Transform target;
	private int MouseWheelSensitivity = 1; //��������������
	private int MouseZoomMin = 1; //���������Сֵ
	private int MouseZoomMax = 20; //����������ֵ

	private float moveSpeed = 10; //��������ٶȣ��м�ƽ��ʱ��������ƽ��ģʽʱ�����ã�Խ�����˶�Խƽ��

	private float xSpeed = 250.0f; //��ת�ӽ�ʱ���x��ת��
	private float ySpeed = 120.0f; //��ת�ӽ�ʱ���y��ת��

	private int yMinLimit = -360;
	private int yMaxLimit = 360;

	private float x = 0.0f; //�洢�����euler��
	private float y = 0.0f; //�洢�����euler��

	private float Distance = 5; //�����target֮��ľ��룬��Ϊ�����Z������ָ��target��Ҳ�������z�᷽���ϵľ���
	private Vector3 targetOnScreenPosition; //Ŀ�����Ļ���꣬������ֵΪz�����
	private Quaternion storeRotation; //�洢�������̬��Ԫ��
	private Vector3 CameraTargetPosition; //target��λ��
	private Vector3 initPosition; //ƽ��ʱ���ڴ洢ƽ�Ƶ����λ��
	private Vector3 cameraX; //�����x�᷽������
	private Vector3 cameraY; //�����y�᷽������
	private Vector3 cameraZ; //�����z�᷽������

	private Vector3 initScreenPos; //�м��հ���ʱ������Ļ���꣨������ֵ��ʵûʲô�ã�
	private Vector3 curScreenPos; //��ǰ������Ļ���꣨������ֵ��ʵûʲô�ã�
	void Start()
	{
		//�����������һ�³�ʼ������ӽ��Լ�һЩ���������������x��y�������Ǻ�����getAxis��mouse x��mouse y��Ӧ
		var angles = transform.eulerAngles;
		x = angles.y;
		y = angles.x;
		CameraTargetPosition = target.position;
		storeRotation = Quaternion.Euler(y + 60, x, 0);
		transform.rotation = storeRotation; //���������̬
		Vector3 position = storeRotation * new Vector3(0.0F, 0.0F, -Distance) + CameraTargetPosition; //��Ԫ����ʾһ����ת����Ԫ�����������൱�ڰ�������ת��Ӧ�Ƕȣ�Ȼ�����Ŀ�������λ�þ������λ����
		transform.position = storeRotation * new Vector3(0, 0, -Distance) + CameraTargetPosition; //�������λ��

		// Debug.Log("Camera x: "+transform.right);
		// Debug.Log("Camera y: "+transform.up);
		// Debug.Log("Camera z: "+transform.forward);

		// //-------------TEST-----------------
		// testScreenToWorldPoint();

	}

	void Update()
	{
		//����Ҽ���ת����
		if (Input.GetMouseButton(1))
		{
			x += Input.GetAxis("Mouse X") * xSpeed * 0.02f;
			y -= Input.GetAxis("Mouse Y") * ySpeed * 0.02f;

			y = ClampAngle(y, yMinLimit, yMaxLimit);

			storeRotation = Quaternion.Euler(y + 60, x, 0);
			var position = storeRotation * new Vector3(0.0f, 0.0f, -Distance) + CameraTargetPosition;

			transform.rotation = storeRotation;
			transform.position = position;
		}
		else if (Input.GetAxis("Mouse ScrollWheel") != 0) //���������Ź���
		{
			if (Distance >= MouseZoomMin && Distance <= MouseZoomMax)
			{
				Distance -= Input.GetAxis("Mouse ScrollWheel") * MouseWheelSensitivity;
			}
			if (Distance < MouseZoomMin)
			{
				Distance = MouseZoomMin;
			}
			if (Distance > MouseZoomMax)
			{
				Distance = MouseZoomMax;
			}
			var rotation = transform.rotation;

			transform.position = storeRotation * new Vector3(0.0F, 0.0F, -Distance) + CameraTargetPosition;
		}

		//����м�ƽ��
		if (Input.GetMouseButtonDown(2))
		{
			cameraX = transform.right;
			cameraY = transform.up;
			cameraZ = transform.forward;

			initScreenPos = new Vector3(Input.mousePosition.x, Input.mousePosition.y, targetOnScreenPosition.z);
			Debug.Log("downOnce");

			//targetOnScreenPosition.zΪĿ�����嵽���xmidbuttonDownPositionyƽ��ķ��߾���
			targetOnScreenPosition = Camera.main.WorldToScreenPoint(CameraTargetPosition);
			initPosition = CameraTargetPosition;
		}

		if (Input.GetMouseButton(2))
		{
			curScreenPos = new Vector3(Input.mousePosition.x, Input.mousePosition.y, targetOnScreenPosition.z);
			//0.01���ϵ���ǿ���ƽ�Ƶ��ٶȣ�Ҫ���������Ŀ�������distance�����ѡ��
			target.position = initPosition - 0.01f * ((curScreenPos.x - initScreenPos.x) * cameraX + (curScreenPos.y - initScreenPos.y) * cameraY);

			//���¼���λ��
			Vector3 mPosition = storeRotation * new Vector3(0.0F, 0.0F, -Distance) + target.position;
			transform.position = mPosition;

			// //��������������ƽ�Ʊ�ø�ƽ�������ǿ�������buttonupʱδʹ����ƶ���Ӧ����λ�ã������ٽ�����ת�����Ų���ʱ���ֶ��ݶ���
			//transform.position=Vector3.Lerp(transform.position,mPosition,Time.deltaTime*moveSpeed);

		}
		if (Input.GetMouseButtonUp(2))
		{
			Debug.Log("upOnce");
			//ƽ�ƽ�����cameraTargetPosition��λ�ø���һ�£���Ȼ��Ӱ����������ת����
			CameraTargetPosition = target.position;
		}

	}

	//��angle������min~max֮��
	static float ClampAngle(float angle, float min, float max)
	{
		if (angle < -360)
			angle += 360;
		if (angle > 360)
			angle -= 360;
		return Mathf.Clamp(angle, min, max);
	}

	void testScreenToWorldPoint()
	{
		//����������ָ���������z��ָ�����ϵľ���
		Vector3 screenPoint = Camera.main.WorldToScreenPoint(CameraTargetPosition);
		Debug.Log("ScreenPoint: " + screenPoint);

		// var worldPosition = Camera.main.ScreenToWorldPoint(new Vector3(1,1,10));
		// Debug.Log("worldPosition: "+worldPosition);
	}
}
