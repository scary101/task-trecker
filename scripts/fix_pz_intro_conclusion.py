from __future__ import annotations

import shutil
import tempfile
import zipfile
from pathlib import Path
from xml.etree import ElementTree as ET


NS = {"w": "http://schemas.openxmlformats.org/wordprocessingml/2006/main"}
W = f"{{{NS['w']}}}"


INTRO = [
    "В настоящее время цифровые инструменты являются важной частью организации проектной деятельности, так как позволяют централизованно хранить данные, распределять задачи и контролировать выполнение работ. При увеличении количества проектов, команд и участников ручное управление становится менее эффективным: возрастает риск потери информации, нарушения сроков и неравномерного распределения нагрузки между сотрудниками.",
    "Современным организациям требуется информационная система, которая объединяет управление проектами, командами, задачами, файлами, рабочими сессиями и коммуникацией участников. Особенно актуальной является возможность разграничения доступа по ролям, так как руководитель организации, руководитель команды, сотрудник и администратор системы выполняют разные функции и должны работать только с доступными им разделами.",
    "В рамках данной работы разрабатывается информационная система StepTreck, предназначенная для управления проектами, командами и задачами организации. Система включает серверную часть с API, веб-клиент и мобильное приложение, что обеспечивает доступ к основным функциям как с рабочего компьютера, так и с мобильного устройства.",
    "StepTreck предоставляет возможность регистрации организации, управления подпиской, создания проектов и команд, назначения руководителей команд, добавления участников, постановки задач, контроля дедлайнов и выполнения чек-листов. Для командной работы реализованы уведомления, командные чаты, файловое хранилище, календарь событий, рабочие сессии, аналитические показатели и журналирование действий.",
    "Таким образом, разработка StepTreck направлена на создание единой платформы для организации проектной работы, повышения прозрачности процессов, улучшения взаимодействия между участниками и упрощения контроля выполнения задач в организации.",
]

CONCLUSION = [
    "В ходе выполнения дипломного проекта была разработана информационная система StepTreck, предназначенная для управления проектами, командами, задачами и рабочей активностью сотрудников организации. В рамках работы были реализованы серверная часть с API, веб-клиент и мобильное приложение, что позволило обеспечить доступ к системе с разных типов устройств.",
    "В системе реализованы регистрация организации и пользователей, авторизация с подтверждением по коду, восстановление пароля, принятие приглашений, разграничение доступа по ролям и управление подпиской. Руководитель организации получил инструменты для работы с проектами, командами, участниками, файлами, чатами и аналитической сводкой. Для руководителя команды реализованы функции управления командой, задачами, участниками, календарём, файлами, статистикой и рабочими сессиями. Сотруднику доступны просмотр и выполнение назначенных задач, работа с чек-листом, файлами, календарём, уведомлениями и командным чатом.",
    "Также в проекте реализованы средства администрирования системы: просмотр системных метрик, журнал аудита, импорт и экспорт данных, резервное копирование, восстановление базы данных и выполнение SQL-запросов. Для хранения данных используется PostgreSQL, для файлового хранилища применяется MinIO, а для контроля состояния сервиса используются средства мониторинга.",
    "В результате разработки была получена функциональная система, которая позволяет централизовать управление проектной деятельностью, сократить количество ручных операций, повысить прозрачность выполнения задач и упростить взаимодействие между участниками организации. Поставленная цель разработки достигнута, а реализованный программный комплекс соответствует требованиям, сформулированным в постановке задачи.",
]


def paragraph_text(p: ET.Element) -> str:
    return "".join(t.text or "" for t in p.findall(".//w:t", NS)).strip()


def make_paragraph(text: str, template: ET.Element) -> ET.Element:
    p = ET.fromstring(ET.tostring(template, encoding="unicode"))
    for child in list(p):
        if child.tag != W + "pPr":
            p.remove(child)

    r = ET.SubElement(p, W + "r")
    t = ET.SubElement(r, W + "t")
    t.set("{http://www.w3.org/XML/1998/namespace}space", "preserve")
    t.text = text
    return p


def replace_section(body: ET.Element, start: str, end: str, texts: list[str]) -> None:
    children = list(body)
    start_idx = next(i for i, el in enumerate(children) if el.tag == W + "p" and paragraph_text(el).upper() == start)
    end_idx = next(i for i in range(start_idx + 1, len(children)) if children[i].tag == W + "p" and paragraph_text(children[i]).upper() == end)

    template = children[start_idx + 1]
    new_paragraphs = [make_paragraph(text, template) for text in texts]

    for el in children[start_idx + 1:end_idx]:
        body.remove(el)

    insert_at = list(body).index(children[start_idx]) + 1
    for offset, paragraph in enumerate(new_paragraphs):
        body.insert(insert_at + offset, paragraph)


def find_ready_doc() -> Path:
    root = Path.home() / "Desktop" / "Diplom"
    for path in root.rglob("*.docx"):
        if path.name.startswith("~$"):
            continue
        if (
            path.parent.name.encode("unicode_escape").decode()
            == "\\u0413\\u043e\\u0442\\u043e\\u0432\\u044b\\u0435 \\u0434\\u043e\\u043a\\u0438"
            and path.name.encode("unicode_escape").decode()
            == "\\u041f\\u043e\\u044f\\u0441\\u043d\\u0438\\u0442\\u0435\\u043b\\u044c\\u043d\\u0430\\u044f \\u0437\\u0430\\u043f\\u0438\\u0441\\u043a\\u0430.docx"
        ):
            return path
    raise FileNotFoundError("Пояснительная записка.docx не найдена")


def write_zip_from_dir(source: Path, output: Path) -> None:
    if output.exists():
        output.unlink()
    with zipfile.ZipFile(output, "w", zipfile.ZIP_DEFLATED) as archive:
        for file in source.rglob("*"):
            if file.is_file():
                archive.write(file, file.relative_to(source).as_posix())


def main() -> None:
    ET.register_namespace("w", NS["w"])
    docx = find_ready_doc()
    backup = docx.with_name(docx.stem + "_до_правки_введения_заключения" + docx.suffix)
    output = docx

    if not backup.exists():
        shutil.copy2(docx, backup)

    with tempfile.TemporaryDirectory() as tmp:
        tmp_path = Path(tmp)
        with zipfile.ZipFile(docx, "r") as archive:
            archive.extractall(tmp_path)

        document_xml = tmp_path / "word" / "document.xml"
        tree = ET.parse(document_xml)
        root = tree.getroot()
        body = root.find("w:body", NS)
        if body is None:
            raise RuntimeError("word/body не найден")

        replace_section(body, "ВВЕДЕНИЕ", "ОБЩАЯ ЧАСТЬ", INTRO)
        replace_section(body, "ЗАКЛЮЧЕНИЕ", "СПИСОК ИСПОЛЬЗУЕМЫХ МАТЕРИАЛОВ", CONCLUSION)

        tree.write(document_xml, encoding="utf-8", xml_declaration=True)

        try:
            write_zip_from_dir(tmp_path, output)
            print(f"Готово: {output}")
        except PermissionError:
            output = docx.with_name(docx.stem + "_исправлено" + docx.suffix)
            write_zip_from_dir(tmp_path, output)
            print(f"Основной файл занят, создана новая версия: {output}")

    print(f"Резервная копия: {backup}")


if __name__ == "__main__":
    main()
