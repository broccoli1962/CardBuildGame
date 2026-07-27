using GoogleSpreadSheetLoader.OneButton;
using UnityEditor;
using UnityEngine;
using static GoogleSpreadSheetLoader.GSSL_State;

namespace GoogleSpreadSheetLoader.Setting
{
    public class IntegratedView
    {
        private bool _isEditMode = false;
        private SettingData _tempSettingData; // 임시 저장용

        public void DrawIntegratedView()
        {
            // SettingData가 초기화되지 않았으면 초기화
            if (_tempSettingData == null)
            {
                InitializeTempData();
            }

            // 윈도우 상단 여백
            EditorGUILayout.Space(5);

            DrawSettingsBox();

            // 박스와 액션 사이 여백
            EditorGUILayout.Space(5);

            DrawActionButtons();

            // 윈도우 하단 여백
            EditorGUILayout.Space(5);
        }

        private void DrawSettingsBox()
        {
            // 박스 좌우 여백
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.Space(5);

            // 설정 박스 시작
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // 설정 제목
            EditorGUILayout.LabelField("설정", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            DrawApiKey();
            DrawSpreadSheetsInfos();
            DrawSheetSettings();

            EditorGUILayout.Space(5);
            DrawEditControls();
            EditorGUILayout.Space(10);

            // 설정 박스 끝
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(5);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawApiKey()
        {
            EditorGUILayout.LabelField("API 키");

            if (_isEditMode)
            {
                _tempSettingData.apiKey = EditorGUILayout.TextField(_tempSettingData.apiKey);
            }
            else
            {
                var displayText = _tempSettingData.apiKey.Length > 10
                    ? _tempSettingData.apiKey.Substring(0, 10) + "..."
                    : _tempSettingData.apiKey;
                EditorGUILayout.LabelField(displayText);
            }

            EditorGUILayout.Space(5);
        }

        private void DrawSpreadSheetsInfos()
        {
            EditorGUILayout.LabelField("스프레드 시트 데이터");

            for (int i = 0; i < _tempSettingData.listSpreadSheetInfo.Count; i++)
            {
                var info = _tempSettingData.listSpreadSheetInfo[i];

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"{i + 1}. ", GUILayout.Width(20));

                if (_isEditMode)
                {
                    info.spreadSheetName = EditorGUILayout.TextField(info.spreadSheetName, GUILayout.Width(100));
                    info.spreadSheetId = EditorGUILayout.TextField(info.spreadSheetId);

                    if (GUILayout.Button("삭제", GUILayout.Width(60)))
                    {
                        _tempSettingData.listSpreadSheetInfo.RemoveAt(i);
                        break;
                    }
                }
                else
                {
                    EditorGUILayout.LabelField(info.spreadSheetName, GUILayout.Width(100));

                    var displayId = info.spreadSheetId.Length > 10
                        ? info.spreadSheetId.Substring(0, 10) + "..."
                        : info.spreadSheetId;
                    EditorGUILayout.LabelField(displayId);

                    if (GUILayout.Button("열기", GUILayout.Width(60)))
                    {
                        Application.OpenURL(string.Format(GSSL_URL.SpreadSheetOpenUrl, info.spreadSheetId, "0"));
                    }
                    
                    // 스프레드시트 단위 최신화 버튼 추가
                    GUI.enabled = CurrState == eGSSL_State.None;
                    if (GUILayout.Button("최신화", GUILayout.Width(60)))
                    {
                        _ = GSSL_OneButton.OneButtonProcessSingleSpreadSheet(info.spreadSheetId);
                    }
                    GUI.enabled = true;
                }

                EditorGUILayout.EndHorizontal();
            }

            if (_isEditMode)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("스프레드시트 추가", GUILayout.Width(150)))
                {
                    _tempSettingData.listSpreadSheetInfo.Add(new SpreadSheetInfo()
                    { spreadSheetName = "Name Here", spreadSheetId = "Id Here" });
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(5);
        }

        private void DrawSheetSettings()
        {
            EditorGUILayout.LabelField("시트 설정");

            if (_isEditMode)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Box(" 시트의 제목에서", GUILayout.Width(100));
                _tempSettingData.sheetTargetStr = EditorGUILayout.TextField(_tempSettingData.sheetTargetStr, GUILayout.Width(50));
                GUILayout.Box("문구가 포함되어 있을 경우, 대상 시트에서 ", GUILayout.Width(250));
                _tempSettingData.sheetTarget = (SettingData.eSheetTargetStandard)EditorGUILayout.EnumPopup(_tempSettingData.sheetTarget, GUILayout.Width(60));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUILayout.Box(" 다음의 문구가 있다면 enum타입을 정의하는 테이블로 분류합니다.");
                _tempSettingData.sheet_enumTypeStr = EditorGUILayout.TextField(_tempSettingData.sheet_enumTypeStr, GUILayout.Width(100));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUILayout.Box(" 다음의 같은 문구가 있다면 Localization테이블로 분류합니다.");
                _tempSettingData.sheet_localizationTypeStr = EditorGUILayout.TextField(_tempSettingData.sheet_localizationTypeStr, GUILayout.Width(100));
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.LabelField($" 시트의 제목에서 \"{_tempSettingData.sheetTargetStr}\" 문구가 포함되어 있을경우, 대상 시트에서 \"{_tempSettingData.sheetTarget.ToString()}\"");
                EditorGUILayout.LabelField($" 다음의 문구가 있다면 enum타입을 정의하는 테이블로 분류합니다. \"{_tempSettingData.sheet_enumTypeStr}\"");
                EditorGUILayout.LabelField($" 다음의 같은 문구가 있다면 Localization테이블로 분류합니다. \"{_tempSettingData.sheet_localizationTypeStr}\"");
            }

            EditorGUILayout.Space(5);
        }

        private void DrawActionButtons()
        {
            if (CurrState != eGSSL_State.None)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField(ProgressText, GUILayout.Width(ProgressText.Length * 12));
                EditorGUILayout.Space(30);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space(30);

                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("취소", GUILayout.Width(180)))
                {
                    GSSL_OneButton.CancelCurrentProcess();
                }
                EditorGUILayout.Space(30);
                EditorGUILayout.EndHorizontal();

                return;
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("최신화 Only", GUILayout.Width(200)))
            {
                _ = GSSL_OneButton.OneButtonProcessSpreadSheet(false);
            }
            EditorGUILayout.Space(30);
            if (GUILayout.Button("클린 & 최신화", GUILayout.Width(200)))
            {
                _ = GSSL_OneButton.OneButtonProcessSpreadSheet(true);
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawEditControls()
        {
            EditorGUILayout.Separator();

            if (!_isEditMode)
            {
                // 편집 모드가 아닐 때 - 편집 버튼만 표시
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("설정 편집", GUILayout.Width(150)))
                {
                    EnterEditMode();
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                // 편집 모드일 때 - 적용/취소 버튼 표시
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("취소", GUILayout.Width(100)))
                {
                    CancelEditMode();
                }

                EditorGUILayout.Space(20);

                if (GUILayout.Button("적용", GUILayout.Width(100)))
                {
                    ApplyChanges();
                }

                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }
        }

        private void InitializeTempData()
        {
            _tempSettingData = new SettingData();
            CopySettingData(GSSL_Setting.SettingData, _tempSettingData);
        }

        private void EnterEditMode()
        {
            _isEditMode = true;
            // 이미 _tempSettingData가 초기화되어 있으므로 추가 작업 불필요
        }

        private void CancelEditMode()
        {
            _isEditMode = false;
            // 원래 설정으로 복원
            CopySettingData(GSSL_Setting.SettingData, _tempSettingData);
        }

        private void ApplyChanges()
        {
            // 임시 설정을 실제 설정에 적용
            CopySettingData(_tempSettingData, GSSL_Setting.SettingData);

            // ScriptableObject에 저장
            EditorUtility.SetDirty(GSSL_Setting.SettingData);
            AssetDatabase.SaveAssets();

            _isEditMode = false;

            Debug.Log("설정이 성공적으로 저장되었습니다.");
        }

        private void CopySettingData(SettingData source, SettingData target)
        {
            target.apiKey = source.apiKey;
            target.sheetTarget = source.sheetTarget;
            target.sheetTargetStr = source.sheetTargetStr;
            target.sheet_enumTypeStr = source.sheet_enumTypeStr;
            target.sheet_localizationTypeStr = source.sheet_localizationTypeStr;

            // 스프레드시트 정보도 깊은 복사
            target.listSpreadSheetInfo.Clear();
            foreach (var info in source.listSpreadSheetInfo)
            {
                target.listSpreadSheetInfo.Add(new SpreadSheetInfo
                {
                    spreadSheetName = info.spreadSheetName,
                    spreadSheetId = info.spreadSheetId
                });
            }
        }
    }
}