"""Create care ScriptableObject assets from Samples/CareContent.json. No Unity launch."""
import json
import re
import uuid
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
GAME = ROOT / "Assets/WorkGame"
DEST = GAME / "Data/Care"
SOURCE = GAME / "Samples/CareContent.json"
STATS = ["Knowledge", "Fitness", "Emotion", "Social", "Logic", "Research", "Focus", "Strength",
         "Endurance", "Reflex", "Imagination", "Expression", "Empathy", "Speech", "Teamwork", "Leadership"]
NAMES = ["지식", "체력", "감성", "사교", "논리력", "탐구력", "집중력", "근력",
         "지구력", "순발력", "상상력", "표현력", "공감력", "화술", "협동심", "리더십"]


def quoted(value):
    return json.dumps(value, ensure_ascii=False)


def guid(path):
    meta = Path(str(path) + ".meta")
    if meta.exists():
        return re.search(r"^guid: (\w+)$", meta.read_text(encoding="utf-8-sig"), re.M)[1]
    return uuid.uuid5(uuid.NAMESPACE_URL, "workgame-care/" + path.relative_to(ROOT).as_posix()).hex


def folder(path):
    path.mkdir(parents=True, exist_ok=True)
    meta = Path(str(path) + ".meta")
    if not meta.exists():
        meta.write_text(f"fileFormatVersion: 2\nguid: {guid(path)}\nfolderAsset: yes\nDefaultImporter:\n"
                        "  externalObjects: {}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n", encoding="utf-8")


def asset_path(kind, item_id):
    if not re.fullmatch(r"[a-z0-9_]+", item_id):
        raise ValueError("Invalid asset ID: " + item_id)
    return DEST / kind / (item_id + ".asset")


def ref(path):
    return "{fileID: 11400000, guid: " + guid(path) + ", type: 2}"


def gains(field, values, amount="Amount"):
    if not values:
        return f"  {field}: []\n"
    result = f"  {field}:\n"
    for stat, value in values.items():
        result += f"  - Stat: {STATS.index(stat)}\n    {amount}: {value}\n"
    return result


def refs(field, paths):
    return f"  {field}:\n" + "".join("  - " + ref(path) + "\n" for path in paths) if paths else f"  {field}: []\n"


def write_asset(path, script, body):
    script_guid = guid(GAME / "Runtime/Data" / (script + ".cs"))
    text = ("%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n--- !u!114 &11400000\nMonoBehaviour:\n"
            "  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n"
            "  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: 0}\n"
            "  m_Enabled: 1\n  m_EditorHideFlags: 0\n"
            f"  m_Script: {{fileID: 11500000, guid: {script_guid}, type: 3}}\n"
            f"  m_Name: {path.stem}\n  m_EditorClassIdentifier: WorkGame::WorkGame.{script}\n" + body)
    path.write_text(text, encoding="utf-8")
    meta = Path(str(path) + ".meta")
    if not meta.exists():
        meta.write_text(f"fileFormatVersion: 2\nguid: {guid(path)}\nNativeFormatImporter:\n"
                        "  externalObjects: {}\n  mainObjectFileID: 11400000\n  userData:\n"
                        "  assetBundleName:\n  assetBundleVariant:\n", encoding="utf-8")


def item_fields(item):
    return (f"  Id: {item['id']}\n  Name: {quoted(item['name'])}\n"
            + (f"  ShortName: {quoted(item['shortName'])}\n" if 'shortName' in item else '')
            + f"  Description: {quoted(item['description'])}\n  Icon: {{fileID: 0}}\n"
            + (f"  ResultLine: {quoted(item['resultLine'])}\n" if 'resultLine' in item else '')
            + (f"  UnlockMonth: {item['month']}\n" if 'month' in item else '') + f"  Cost: {item['cost']}\n")


def main():
    data = json.loads(SOURCE.read_text(encoding="utf-8-sig"))
    folder(DEST)
    for kind in ["Lessons", "Synergies", "Gifts", "Endings"]:
        folder(DEST / kind)
    lessons = {x["id"]: asset_path("Lessons", x["id"]) for x in data["lessons"]}
    synergies = list(data["synergies"])
    for item in data["lessons"]:
        path = lessons[item["id"]]
        picture = "  ResultImage: {fileID: 0}\n"
        if path.exists():
            match = re.search(r"^  ResultImage:.*$", path.read_text(encoding="utf-8-sig"), re.M)
            if match:
                picture = match.group(0) + "\n"
        write_asset(path, "CareLessonSO", item_fields(item) + picture + gains("Gains", item["gains"]))
    for item in synergies:
        write_asset(asset_path("Synergies", item["id"]), "CareSynergySO",
                    f"  Id: {item['id']}\n  Name: {quoted(item['name'])}\n  Description: {quoted(item['description'])}\n"
                    + refs("Lessons", [lessons[x] for x in item["lessons"]]) + gains("Gains", item["gains"]))
    for item in data["gifts"]:
        write_asset(asset_path("Gifts", item["id"]), "CareGiftSO", item_fields(item)
                    + f"  Kind: {['Desk','Equipment','Chair'].index(item['kind'])}\n"
                    + f"  Friendship: {item['friendship']}\n" + gains("Bonuses", item["bonuses"], "Percent"))
    for item in data["endings"]:
        body = (f"  Id: {item['id']}\n  Name: {quoted(item['name'])}\n  Priority: {item['priority']}\n"
                + gains("Needs", item["needs"]))
        body += f"  ChoiceText: {quoted(item.get('choiceText', '달이가 새로운 길을 선택합니다.'))}\n"
        body += "  MainNeeds:" + ("\n" if item['mainNeeds'] else " []\n")
        for stat, grade in item['mainNeeds'].items():
            body += f"  - Stat: {STATS.index(stat)}\n    Grade: {['None','B','A','S'].index(grade)}\n"
        for field in ["Text", "LowText", "NormalText", "HighText"]:
            key = field[0].lower() + field[1:]
            body += f"  {field}: {quoted(item[key])}\n"
        ending_path = asset_path("Endings", item["id"])
        # Page images and letter staging are edited in Unity, separately from JSON story text.
        if ending_path.exists():
            previous = ending_path.read_text(encoding="utf-8-sig")
            for match in re.finditer(r"^  (?:Pages|AppendFriendshipText|CollectionImage):[^\n]*(?:\n(?!  [A-Za-z_]\w*:|---)[^\n]+)*", previous, re.M):
                body += match.group(0) + "\n"
        write_asset(ending_path, "CareEndingSO", body)
    fallback = next(x for x in data["endings"] if x.get("fallback"))
    config = (refs("Lessons", list(lessons.values()))
              + refs("Synergies", [asset_path("Synergies", x["id"]) for x in synergies])
              + refs("Gifts", [asset_path("Gifts", x["id"]) for x in data["gifts"]])
              + refs("Endings", [asset_path("Endings", x["id"]) for x in data["endings"] if not x.get("fallback")])
              + "  FallbackEnding: " + ref(asset_path("Endings", fallback["id"])) + "\n"
              + "  InitialStats: []\n  InitialFriendship: 0\n  LowFriendshipMax: -1\n  HighFriendshipMin: 50\n"
              + "  Dialogues: {fileID: 4900000, guid: " + guid(GAME / 'Samples/CareDialogue.json') + ", type: 3}\n"
              + "  Grades:\n" + ''.join(f"    {grade}: {value}\n" for grade,value in data['grades'].items())
              + f"  StatMax: {data.get('statMax', 300)}\n"
              + gains("HomeGains", {stat: 1 for stat in ['Focus','Endurance','Empathy','Teamwork']}) + "  RestGains: []\n")
    write_asset(DEST / "CareConfig.asset", "CareConfigSO", config)
    write_guide(data, synergies)
    source_meta = Path(str(SOURCE) + ".meta")
    if not source_meta.exists():
        source_meta.write_text(f"fileFormatVersion: 2\nguid: {guid(SOURCE)}\nTextScriptImporter:\n"
                               "  externalObjects: {}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n", encoding="utf-8")
    print(f"Created {len(lessons)} lessons, {len(synergies)} synergies, {len(data['gifts'])} gifts, "
          f"{len(data['endings'])} endings / {len(data['endings']) * 3} relationship epilogues, and CareConfig.")


def write_guide(data, synergies):
    name = dict(zip(STATS, NAMES))
    lesson_names = {x["id"]: x["name"] for x in data["lessons"]}
    lines = ["# 육성 콘텐츠", "", f"32개 수업 + 무료 홈스쿨링·휴식 / 시너지 {len(synergies)}개 / 방에 놓는 선물 12개 / 엔딩 25개 + 후일담 75개.", "",
             "다람쥐 달이의 익숙한 공부·운동·예술·모둠 활동에 한국식 선행학습과 특강 열기를 얹었습니다.",
             "1월 입문 → 3월 심화 → 6월 실전 → 9월 실전 특강입니다. 9월은 별도 상위 등급이 아닌 실전 과정의 전문 특강입니다.", "",
             "연결할 SO: `Assets/WorkGame/Data/Care/CareConfig.asset`. UI와 씬은 자동 변경하지 않습니다.", "",
             "원본은 `Samples/CareContent.json`입니다. SO에서 직접 수정해도 됩니다. 원본을 일괄 반영하려면 프로젝트 루트에서",
             "`python Tools/build_care_data.py`를 실행합니다. 기존 기본 SO 값은 원본 내용으로 덮어쓰며 참조 GUID는 유지합니다.", "",
             "## 수업", "", "각 해금 시점에 주요 스탯별 2개, 총 8개씩 추가합니다. 12월 추가 해금은 없습니다.", "",
             "수업 자체가 세부 스탯을 올립니다. 대표 스탯은 소속 세부 스탯 3개의 합이며 별도로 증가시키지 않습니다.", "",
             "| 해금 월 | 과정 | 계열 | 수업 | 세부 성장 | 비용 |", "| --- | --- | --- | --- | --- | ---: |"]
    stages = {1: "입문", 3: "심화", 6: "실전", 9: "실전 특강"}
    for x in data["lessons"]:
        main_stat = STATS[(STATS.index(next(iter(x['gains']))) - 4) // 3]
        gain_text = ', '.join(f"{name[stat]} +{value}" for stat,value in x['gains'].items())
        lines.append(f"| {x['month']} | {stages[x['month']]} | {name[main_stat]} | {x['name']} | {gain_text} | {x['cost']} |")
    lines += ["", "## 시너지", "", "월 4칸 전체에서 검사하며 순서 무관입니다. 같은 시너지는 월 1회만 발동하고 서로 다른 시너지는 함께 적용합니다.", "",
              "| 이름 | 필요한 수업 | 추가 성장 |", "| --- | --- | --- |"]
    for x in synergies:
        bonus = ", ".join(f"{name[k]} +{v}" for k, v in x["gains"].items())
        lines.append(f"| {x['name']} | {' + '.join(lesson_names[i] for i in x['lessons'])} | {bonus} |")
    lines += ["", "## 선물", "", "전부 방에 놓을 수 있는 물품이며 옷·착용 장비·여행권은 없습니다. 방 오브젝트의 실제 배치는 이후 UI 작업에서 연결합니다.",
              "선물의 해금 월은 없습니다. 첫 달부터 가격만 충족하면 구매합니다.",
              "책상·장비·의자별 1개씩, 최대 3개 활성화합니다. 새로 구매하면 같은 종류의 활성 물품을 교체하며 기존 물품은 계속 보유합니다.",
              "보정은 활성 물품만 적용합니다. 대표 +%는 소속 세부 3개에 적용하고, 직접 세부 +%와 합산합니다. 친밀도는 최초 구매 시에만 변합니다.", "",
              "| 종류 | 선물 | 가격 | 친밀도 | 성장 보정 |", "| --- | --- | ---: | ---: | --- |"]
    kind_names = {'Desk':'책상','Equipment':'장비','Chair':'의자'}
    for x in data["gifts"]:
        bonus = ", ".join(f"{name[k]} +{v}%" for k, v in x["bonuses"].items())
        lines.append(f"| {kind_names[x['kind']]} | {x['name']} | {x['cost']} | {x['friendship']:+} | {bonus} |")
    lines += ["", "## 엔딩", "", "큰 Priority가 먼저이며 같은 우선순위는 ID 순입니다. 모든 스탯 조건을 충족해야 합니다.",
              "대통령 500 → 조달청 공무원 400 → 주요 특화 300 → 주요 조합 250 → 세부 특화 200 → 백수(조건 미충족) 순입니다.", "",
              "| 직업 | 우선순위 | 최소 스탯 조건 |", "| --- | ---: | --- |"]
    for x in data["endings"]:
        conditions = [f"{name[k]} {v} 이상" for k,v in x['mainNeeds'].items()]
        conditions += [f"{name[k]} {v}" for k,v in x['needs'].items()]
        needs = ', '.join(conditions) or "다른 조건 미충족"
        lines.append(f"| {x['name']} | {x['priority']} | {needs} |")
    lines += ["", "대표 스탯 등급은 합산값 기준 " + ' / '.join(f"{grade} {value} 이상" for grade,value in data['grades'].items()) + "입니다. B 미만은 미달(None)입니다.",
              "등급 기준은 CareConfig의 Grades에서 조절합니다. 엔딩 SO는 대표 최소 등급 + 세부 최소 수치로 판정합니다.",
              f"세부 스탯 실제 상한은 {data.get('statMax', 300)}, 대표 합산 최대는 {data.get('statMax', 300)*3}입니다. CareConfig.StatMax에서 조절합니다.",
              "각 직업의 본문과 친밀도 낮음·보통·높음 후일담은 엔딩 SO에 모두 작성했습니다.",
              "친밀도 임시 경계는 -1 이하 낮음 / 0~49 보통 / 50 이상 높음입니다. 퇴근·대화·약속 수치는 Samples/CareDialogue.json에서 관리합니다.", "",
              "## 초기 밸런스", "", "월급 기본 1,000 기준 수업 1회 비용은 100 → 180 → 260 → 340입니다.",
              "월 4회 최상위 수업은 1,360이므로 분기 보너스와 인맥 할인에 의미가 생깁니다.",
              "홈스쿨링은 무료로 집중력·지구력·공감력·협동심 각각 +1이므로 대표 스탯도 각각 +1입니다. 휴식은 현재 효과 없이 편성만 가능합니다.",
              "수업·선물·시너지에 따른 성장과 엔딩 조건은 초기값입니다. 플레이 밸런스와 엔딩별 도달 여부는 검증하지 않았습니다.",
              "컴파일·유닛 테스트·플레이 테스트는 실행하지 않았습니다.", ""]
    guide = GAME / "CareContent.md"
    guide.write_text("\n".join(lines), encoding="utf-8")
    meta = Path(str(guide) + ".meta")
    if not meta.exists():
        meta.write_text(f"fileFormatVersion: 2\nguid: {guid(guide)}\nTextScriptImporter:\n"
                        "  externalObjects: {}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n", encoding="utf-8")


if __name__ == "__main__":
    main()
