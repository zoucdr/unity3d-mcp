# Unity3d MCP ��Ŀ Copilot ָ��

## ��Ŀ�ܹ������֪ʶ
- **�ֲ�ܹ�**��AI�ͻ��ˣ�Cursor/Claude/Trae���� MCPЭ��㣨Python Server + Unity Package���� ͨ�Ų㣨TCP Socket, JSON-RPC���� Unity�༭���� �� ���߲㣨32+����/״̬����
- **��ҪĿ¼**��
  - `server/`��Python MCP����ˣ�����FastMCP�����Ϊ`server.py`�����ü�`pyproject.toml`��`requirements.txt`
  - `unity-package/`��Unity�༭�������C#ʵ�֣���������MCP�����봰��
  - `unity3d/`��Unity��ĿԴ�롢�����UI�����ߵ�
  - `docs/`����̬��վ���ĵ�

## �������������
- **��������**��
  - Python����ˣ�`uv --directory server run server.py`�����`README.md`/`package.json`��
  - Unity�ˣ�����`unity-package`��ͨ��`Window > MCP`�˵����ʹ��ߴ���
- **���Թ���**��
  - `McpDebugWindow`��`unity-package/Editor/GUI/`�������ڲ��Ժ͵���MCP�������ã�֧��JSON����ͽ��Ԥ��
  - ��־���ƣ�`README_LogControl.md`��ͳһͨ��`McpLogger.EnableLog`���أ����ó־û���`EditorPrefs`����������ļ����������
- **Э��ϵͳ**��
  - ����`EditorApplication.update`��������`MainThreadExecutor`��`CoroutineManager`��`StateTreeContext`�����`README_Coroutines.md`
  - ֧������/�ӳ�/�ظ�/����/�첽Э�̣�����Э�������߳�ִ�У������л��Զ�ֹͣ

## ����/���ߵ���Լ��
- **MCP���ߵ���**��
  - �Ƽ�ͨ��`async_call`��`batch_call`����`.cursor/rules/unity-mcp.mdc`��������MethodTools��ͨ��FacadeTools����������
  - `async_call`֧���첽���ã�`type='in'`ִ������`type='out'`��ȡ�������Ҫ`id`������ʶ����
  - GMָ��/��������ͳһ��`gm_command`����
- **UI����**��
  - UI Toolkitʾ����`Assets/UIToolkit/README.md`��UXML/USS/Controller���룬ͼƬ��Դ�谴Figma�ڵ�ID����
  - Բ��UI�Ƽ���`ProceduralUIImage`����`unity_package_proceduraluiimage/README.md`��

## ��ĿԼ����ע������
- Python�������ɵ�`server/`��C#���ɵ�`Assets/Scripts/`
- ��ֹ��`try-catch`��д`yield`����`MonoBehaviour`���ֱֹ����`Destroy`��Unity���÷���
- ��־���������`EnableLog`ͳһ���ƣ�������־���������
- Unity�汾�Ƽ�`2021.3.x`����ƽ̨֧��Win/Mac/Android/iOS

## �ؼ��ļ�/Ŀ¼�ο�
- `server/server.py`��`unity-package/Editor/GUI/McpDebugWindow.cs`��`unity-package/README_LogControl.md`��`unity-package/README_Coroutines.md`��`unity3d/.cursor/rules/unity-mcp.mdc`��`Assets/UIToolkit/README.md`

---
���в��������©�Ĳ��֣��뷴���Ա㲹�����ơ�
