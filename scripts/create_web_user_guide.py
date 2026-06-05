from __future__ import annotations

import html
import shutil
import zipfile
from pathlib import Path


OUTPUT = Path.home() / "Desktop" / "Diplom" / "ПРИЛОЖЕНИЕ В. Руководство пользователя.docx"
BACKUP = OUTPUT.with_name("ПРИЛОЖЕНИЕ В. Руководство пользователя_старое.docx")


def esc(text: str) -> str:
    return html.escape(text, quote=False)


def p(text: str = "", style: str = "Normal") -> str:
    text = esc(text)
    return f'<w:p><w:pPr><w:pStyle w:val="{style}"/></w:pPr><w:r><w:t xml:space="preserve">{text}</w:t></w:r></w:p>'


def page_break() -> str:
    return '<w:p><w:r><w:br w:type="page"/></w:r></w:p>'


def table(rows: list[list[str]]) -> str:
    xml = ['<w:tbl><w:tblPr><w:tblW w:w="5000" w:type="pct"/>']
    xml.append('<w:tblBorders>')
    for name in ("top", "left", "bottom", "right", "insideH", "insideV"):
        xml.append(f'<w:{name} w:val="single" w:sz="4" w:space="0" w:color="000000"/>')
    xml.append('</w:tblBorders></w:tblPr>')
    for row in rows:
        xml.append("<w:tr>")
        for cell in row:
            xml.append("<w:tc><w:tcPr><w:tcMar>")
            for name, width in (("top", "57"), ("bottom", "57"), ("left", "85"), ("right", "85")):
                xml.append(f'<w:{name} w:w="{width}" w:type="dxa"/>')
            xml.append("</w:tcMar></w:tcPr>")
            xml.append(p(cell, "TableText"))
            xml.append("</w:tc>")
        xml.append("</w:tr>")
    xml.append("</w:tbl>")
    return "".join(xml)


class Builder:
    def __init__(self) -> None:
        self.parts: list[str] = []
        self.figure_number = 1

    def add(self, text: str = "", style: str = "Normal") -> None:
        self.parts.append(p(text, style))

    def h1(self, text: str) -> None:
        self.add(text, "Heading1")

    def h2(self, text: str) -> None:
        self.add(text, "Heading2")

    def figure(self, title: str, intro: str | None = None) -> None:
        number = f"В.{self.figure_number}"
        if intro is None:
            intro = f"На рисунке {number} представлен экран «{title}»."
        self.add(intro)
        self.add("[Место для вставки снимка экрана]", "Placeholder")
        self.add(f"Рисунок {number} - {title}", "Caption")
        self.figure_number += 1


def build_body() -> str:
    b = Builder()

    b.add("ПРИЛОЖЕНИЕ В. РУКОВОДСТВО ПОЛЬЗОВАТЕЛЯ", "Title")
    b.add("АННОТАЦИЯ", "Heading1")
    b.add(
        "В данном программном документе приведено руководство пользователя Web-сервиса StepTreck. "
        "Документ описывает назначение системы, условия её эксплуатации, порядок запуска и основные "
        "сценарии работы пользователей с разными ролями."
    )
    b.add(
        "В разделе «Назначение программы» указано функциональное назначение Web-сервиса. "
        "В разделе «Условия выполнения программы» приведены требования к рабочему месту пользователя. "
        "В разделе «Выполнение программы» описаны действия неавторизованного пользователя, руководителя "
        "организации, тимлида, сотрудника и администратора системы. В разделе «Сообщения оператору» "
        "перечислены основные сообщения, которые выводятся во время работы с системой."
    )

    b.add("СОДЕРЖАНИЕ", "Heading1")
    for line in (
        "1. НАЗНАЧЕНИЕ ПРОГРАММЫ",
        "2. УСЛОВИЯ ВЫПОЛНЕНИЯ ПРОГРАММЫ",
        "3. ВЫПОЛНЕНИЕ ПРОГРАММЫ",
        "3.1. Запуск Web-сервиса",
        "3.2. Работа неавторизованного пользователя",
        "3.3. Общие действия авторизованного пользователя",
        "3.4. Работа руководителя организации",
        "3.5. Работа тимлида",
        "3.6. Работа сотрудника",
        "3.7. Работа с командным чатом",
        "3.8. Работа администратора системы",
        "4. СООБЩЕНИЯ ОПЕРАТОРУ",
    ):
        b.add(line)

    b.h1("1. НАЗНАЧЕНИЕ ПРОГРАММЫ")
    b.add(
        "Web-сервис StepTreck предназначен для управления проектами, командами и задачами организации. "
        "Система обеспечивает регистрацию организации, подключение участников, управление подпиской, "
        "создание проектов и команд, назначение ответственных лиц, ведение задач, контроль дедлайнов, "
        "работу с чек-листами, файлами, календарём событий, уведомлениями, рабочими сессиями и командным чатом."
    )
    b.add(
        "Пользователями Web-сервиса являются руководитель организации, тимлид, сотрудник и администратор системы. "
        "Доступ к разделам определяется ролью пользователя. После авторизации пользователь переходит в рабочее "
        "пространство, где отображаются доступные ему функции."
    )

    b.h1("2. УСЛОВИЯ ВЫПОЛНЕНИЯ ПРОГРАММЫ")
    b.add("В таблице В.1 представлены минимальные и рекомендуемые требования для работы с Web-сервисом StepTreck.")
    b.add("Таблица В.1 - Требования к рабочему месту пользователя", "Caption")
    b.parts.append(table([
        ["№", "Тип требования", "Минимальное значение", "Рекомендуемое значение"],
        ["1", "Устройство", "Персональный компьютер или ноутбук", "Персональный компьютер или ноутбук"],
        ["2", "Браузер", "Современный браузер с поддержкой JavaScript", "Google Chrome, Microsoft Edge или Mozilla Firefox актуальной версии"],
        ["3", "Экран", "Разрешение от 1280x720", "Разрешение от 1920x1080"],
        ["4", "Оперативная память", "2 ГБ", "8 ГБ"],
        ["5", "Интернет-соединение", "Стабильное подключение к сети", "Широкополосное подключение к сети"],
    ]))

    b.h1("3. ВЫПОЛНЕНИЕ ПРОГРАММЫ")
    b.h2("3.1. Запуск Web-сервиса")
    b.add(
        "Для начала работы пользователь открывает Web-сервис StepTreck в браузере. После загрузки отображается "
        "главная страница с навигацией по сервису, информацией о возможностях платформы и тарифах."
    )
    b.figure("Главная страница Web-сервиса StepTreck")
    b.add(
        "С главной страницы пользователь может перейти к тарифам, конфигуратору подписки, странице входа или "
        "странице регистрации организации."
    )
    b.figure("Блок тарифов на главной странице")
    b.figure("Конфигуратор подписки")

    b.h2("3.2. Работа неавторизованного пользователя")
    b.add(
        "Неавторизованный пользователь может просматривать общую информацию о сервисе, выбирать тариф, "
        "создавать организацию, регистрироваться как участник по приглашению, входить в систему и восстанавливать пароль."
    )
    b.figure("Форма регистрации организации")
    b.add(
        "Для регистрации организации пользователь указывает название организации, фамилию, имя, отчество при наличии, "
        "корпоративную почту, пароль и подтверждение пароля. Перед отправкой формы необходимо принять условия "
        "пользовательского соглашения и политики конфиденциальности."
    )
    b.figure("Форма регистрации участника")
    b.add(
        "Регистрация участника используется для создания личной учётной записи. Для подключения к организации "
        "пользователь должен принять приглашение или перейти по ссылке приглашения."
    )
    b.figure("Страница входа")
    b.add(
        "Для входа пользователь вводит корпоративную почту и пароль, после чего запрашивает код подтверждения. "
        "Код отправляется на указанную почту."
    )
    b.figure("Форма ввода кода подтверждения")
    b.add(
        "После ввода корректного кода пользователь авторизуется в системе и перенаправляется в рабочее пространство."
    )
    b.figure("Форма запроса восстановления пароля")
    b.add(
        "Если пароль утрачен, пользователь указывает почту на странице восстановления пароля. После получения ссылки "
        "пользователь переходит на страницу смены пароля и задаёт новый пароль."
    )
    b.figure("Страница смены пароля")
    b.figure("Страница принятия приглашения")

    b.h2("3.3. Общие действия авторизованного пользователя")
    b.add(
        "После авторизации пользователю доступны профиль, уведомления, настройки аккаунта, смена темы интерфейса, "
        "выход из аккаунта и переход в рабочее пространство. Состав разделов рабочего пространства зависит от роли."
    )
    b.figure("Рабочее пространство авторизованного пользователя")
    b.figure("Профиль пользователя")
    b.add(
        "В профиле пользователь может просмотреть данные аккаунта, изменить отображаемые сведения, обновить фотографию "
        "профиля и выйти из системы."
    )
    b.figure("Журнал уведомлений пользователя")

    b.h2("3.4. Работа руководителя организации")
    b.add(
        "Руководитель организации управляет организацией, подпиской, проектами, командами, участниками, файлами, "
        "чатами и аналитической сводкой. Основной экран роли содержит показатели по организации и быстрый переход "
        "к ключевым разделам."
    )
    b.figure("Сводка руководителя организации")
    b.figure("Страница управления подпиской")
    b.add(
        "В разделе подписки руководитель просматривает текущий тариф, ограничения по участникам, проектам и командам, "
        "а также переходит к оформлению или продлению подписки."
    )
    b.figure("Страница списка проектов")
    b.add(
        "Для создания проекта руководитель нажимает кнопку создания проекта, вводит название, описание и при необходимости "
        "ссылку на Git-репозиторий."
    )
    b.figure("Окно создания проекта")
    b.figure("Страница проекта")
    b.add(
        "На странице проекта отображаются сведения о проекте, ссылка на репозиторий, действия редактирования и переходы "
        "к дашборду, командам, участникам, файлам и журналу проекта."
    )
    b.figure("Окно редактирования проекта")
    b.figure("Дашборд проекта")
    b.figure("Страница команд проекта")
    b.add(
        "В разделе команд руководитель создаёт команды проекта, просматривает список команд и открывает карточку нужной команды."
    )
    b.figure("Окно создания команды")
    b.figure("Страница участников проекта")
    b.figure("Страница файлов проекта")
    b.add(
        "В файловом разделе пользователь выбирает файл, загружает его в хранилище, скачивает ранее загруженные файлы "
        "или удаляет ненужные материалы."
    )
    b.figure("Журнал действий проекта")
    b.figure("Страница участников организации")
    b.add(
        "В разделе участников организации руководитель просматривает сотрудников, открывает профиль участника, "
        "отправляет приглашения и управляет доступом пользователей."
    )
    b.figure("Окно отправки приглашения участнику")
    b.figure("Список командных чатов организации")

    b.h2("3.5. Работа тимлида")
    b.add(
        "Тимлид работает с назначенной командой. Ему доступны сведения о команде, дашборд, участники, статистика, "
        "календарь, задачи, файлы, чат, рабочие сессии и история действий команды."
    )
    b.figure("Страница команды")
    b.figure("Дашборд команды")
    b.figure("Страница участников команды")
    b.add(
        "На странице участников тимлид просматривает состав команды, добавляет участников, назначает роли и открывает "
        "профили сотрудников."
    )
    b.figure("Страница статистики команды")
    b.figure("Календарь команды")
    b.add(
        "Календарь используется для просмотра событий, дедлайнов задач и важных дат команды."
    )
    b.figure("Страница задач команды")
    b.add(
        "В разделе задач тимлид создаёт задачи, назначает исполнителя, задаёт приоритет, дедлайн, описание и чек-лист."
    )
    b.figure("Окно создания задачи")
    b.figure("Страница подробной информации о задаче")
    b.add(
        "На странице задачи доступны просмотр деталей, изменение задачи, управление чек-листом, загрузка файлов и завершение задачи."
    )
    b.figure("Страница истории задач команды")
    b.figure("Страница файлов команды")
    b.figure("Страница активных рабочих сессий")
    b.figure("Страница истории рабочих сессий")

    b.h2("3.6. Работа сотрудника")
    b.add(
        "Сотрудник использует Web-сервис для просмотра назначенных задач, выполнения чек-листа, работы с файлами, "
        "календарём, заметками, уведомлениями, командным чатом и собственными рабочими сессиями."
    )
    b.figure("Страница активных задач сотрудника")
    b.figure("Страница задачи сотрудника")
    b.add(
        "Для выполнения задачи сотрудник открывает задачу, просматривает описание и дедлайн, отмечает пункты чек-листа "
        "и завершает задачу после выполнения всех обязательных пунктов."
    )
    b.figure("Чек-лист задачи")
    b.figure("История задач сотрудника")
    b.figure("Страница файлов команды для сотрудника")
    b.figure("Календарь событий сотрудника")
    b.figure("Страница личной истории рабочих сессий")
    b.figure("Страница заметок")

    b.h2("3.7. Работа с командным чатом")
    b.add(
        "Командный чат доступен пользователям, которые состоят в соответствующей команде. В чате можно отправлять "
        "сообщения, отвечать на сообщения, ставить реакции, закреплять важные сообщения, выполнять поиск и использовать "
        "упоминания участников."
    )
    b.figure("Командный чат")
    b.figure("Поиск сообщений в командном чате")
    b.figure("Закреплённые сообщения командного чата")

    b.h2("3.8. Работа администратора системы")
    b.add(
        "Администратор системы контролирует состояние сервиса и выполняет служебные операции. Ему доступны системные "
        "метрики, журнал аудита, инструменты импорта и экспорта данных, резервное копирование, восстановление базы данных "
        "и выполнение SQL-запросов."
    )
    b.figure("Страница системных метрик")
    b.figure("Журнал аудита системы")
    b.figure("Страница инструментов работы с данными")
    b.figure("Форма выполнения SQL-запроса")

    b.h1("4. СООБЩЕНИЯ ОПЕРАТОРУ")
    b.add("В таблице В.2 представлены основные сообщения, которые могут выводиться пользователю во время работы с Web-сервисом.")
    b.add("Таблица В.2 - Сообщения оператору", "Caption")
    b.parts.append(table([
        ["№", "Ситуация", "Сообщение или реакция системы"],
        ["1", "Пользователь ввёл некорректную почту", "Отображается сообщение о неверном формате email."],
        ["2", "Пользователь не указал пароль", "Отображается сообщение о необходимости заполнить пароль."],
        ["3", "Код подтверждения не введён", "Отображается сообщение о необходимости ввести код."],
        ["4", "Код подтверждения неверный или истёк", "Отображается сообщение об ошибке и доступна повторная отправка кода."],
        ["5", "Повторная отправка кода запрошена слишком часто", "Кнопка отправки временно блокируется, отображается таймер."],
        ["6", "Пароли при регистрации или смене пароля не совпадают", "Отображается сообщение о несовпадении паролей."],
        ["7", "Не приняты соглашения при регистрации организации", "Отправка формы запрещается до установки обязательных флажков."],
        ["8", "Название проекта или команды не заполнено", "Отображается сообщение о необходимости заполнить название."],
        ["9", "Дедлайн задачи указан в прошлом", "Отображается сообщение о невозможности сохранить прошлую дату."],
        ["10", "Загружаемый файл превышает допустимый размер", "Отображается сообщение о превышении лимита загрузки."],
        ["11", "У пользователя недостаточно прав", "Пользователь перенаправляется на допустимую страницу или получает сообщение об ограничении доступа."],
        ["12", "Операция выполнена успешно", "Отображается уведомление об успешном сохранении или выполнении действия."],
    ]))

    return "".join(b.parts)


def styles_xml() -> str:
    return '''<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<w:styles xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
  <w:style w:type="paragraph" w:default="1" w:styleId="Normal">
    <w:name w:val="Normal"/>
    <w:pPr><w:jc w:val="both"/><w:spacing w:line="360" w:lineRule="auto" w:before="0" w:after="0"/><w:ind w:firstLine="708"/></w:pPr>
    <w:rPr><w:rFonts w:ascii="Times New Roman" w:hAnsi="Times New Roman" w:cs="Times New Roman"/><w:sz w:val="28"/></w:rPr>
  </w:style>
  <w:style w:type="paragraph" w:styleId="Title">
    <w:name w:val="Title"/><w:basedOn w:val="Normal"/>
    <w:pPr><w:jc w:val="center"/><w:spacing w:before="0" w:after="240"/></w:pPr>
    <w:rPr><w:b/><w:rFonts w:ascii="Times New Roman" w:hAnsi="Times New Roman" w:cs="Times New Roman"/><w:sz w:val="28"/></w:rPr>
  </w:style>
  <w:style w:type="paragraph" w:styleId="Heading1">
    <w:name w:val="heading 1"/><w:basedOn w:val="Normal"/>
    <w:pPr><w:jc w:val="center"/><w:spacing w:before="240" w:after="160"/></w:pPr>
    <w:rPr><w:b/><w:allCaps/><w:rFonts w:ascii="Times New Roman" w:hAnsi="Times New Roman" w:cs="Times New Roman"/><w:sz w:val="28"/></w:rPr>
  </w:style>
  <w:style w:type="paragraph" w:styleId="Heading2">
    <w:name w:val="heading 2"/><w:basedOn w:val="Normal"/>
    <w:pPr><w:jc w:val="left"/><w:spacing w:before="200" w:after="120"/><w:ind w:firstLine="708"/></w:pPr>
    <w:rPr><w:b/><w:rFonts w:ascii="Times New Roman" w:hAnsi="Times New Roman" w:cs="Times New Roman"/><w:sz w:val="28"/></w:rPr>
  </w:style>
  <w:style w:type="paragraph" w:styleId="Caption">
    <w:name w:val="Caption"/><w:basedOn w:val="Normal"/>
    <w:pPr><w:jc w:val="center"/><w:spacing w:before="80" w:after="160"/><w:ind w:firstLine="0"/></w:pPr>
    <w:rPr><w:rFonts w:ascii="Times New Roman" w:hAnsi="Times New Roman" w:cs="Times New Roman"/><w:sz w:val="28"/></w:rPr>
  </w:style>
  <w:style w:type="paragraph" w:styleId="Placeholder">
    <w:name w:val="Placeholder"/><w:basedOn w:val="Normal"/>
    <w:pPr><w:jc w:val="center"/><w:spacing w:before="120" w:after="120"/><w:ind w:firstLine="0"/></w:pPr>
    <w:rPr><w:i/><w:color w:val="777777"/><w:rFonts w:ascii="Times New Roman" w:hAnsi="Times New Roman" w:cs="Times New Roman"/><w:sz w:val="28"/></w:rPr>
  </w:style>
  <w:style w:type="paragraph" w:styleId="TableText">
    <w:name w:val="TableText"/><w:basedOn w:val="Normal"/>
    <w:pPr><w:jc w:val="center"/><w:spacing w:line="240" w:lineRule="auto" w:before="0" w:after="0"/><w:ind w:firstLine="0"/></w:pPr>
    <w:rPr><w:rFonts w:ascii="Times New Roman" w:hAnsi="Times New Roman" w:cs="Times New Roman"/><w:sz w:val="24"/></w:rPr>
  </w:style>
</w:styles>
'''


def document_xml(body: str) -> str:
    return f'''<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
  <w:body>
    {body}
    <w:sectPr>
      <w:pgSz w:w="11906" w:h="16838"/>
      <w:pgMar w:top="1134" w:right="567" w:bottom="1134" w:left="1701" w:header="708" w:footer="708" w:gutter="0"/>
    </w:sectPr>
  </w:body>
</w:document>
'''


def write_docx(path: Path, body: str) -> None:
    content_types = '''<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
  <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
  <Default Extension="xml" ContentType="application/xml"/>
  <Override PartName="/word/document.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml"/>
  <Override PartName="/word/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.styles+xml"/>
</Types>
'''
    rels = '''<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="word/document.xml"/>
</Relationships>
'''
    doc_rels = '''<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/>
</Relationships>
'''
    if path.exists():
        path.unlink()
    with zipfile.ZipFile(path, "w", zipfile.ZIP_DEFLATED) as z:
        z.writestr("[Content_Types].xml", content_types)
        z.writestr("_rels/.rels", rels)
        z.writestr("word/document.xml", document_xml(body))
        z.writestr("word/styles.xml", styles_xml())
        z.writestr("word/_rels/document.xml.rels", doc_rels)


def main() -> None:
    if OUTPUT.exists() and not BACKUP.exists():
        shutil.copy2(OUTPUT, BACKUP)
    body = build_body()
    try:
        write_docx(OUTPUT, body)
        print(f"Готово: {OUTPUT}")
        print(f"Резервная копия старого файла: {BACKUP}")
    except PermissionError:
        fallback = OUTPUT.with_name("ПРИЛОЖЕНИЕ В. Руководство пользователя WEB StepTreck.docx")
        write_docx(fallback, body)
        print(f"Основной файл занят, создана новая версия: {fallback}")


if __name__ == "__main__":
    main()
