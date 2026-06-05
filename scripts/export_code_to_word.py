from __future__ import annotations

import argparse
import html
import os
import zipfile
from pathlib import Path


CODE_EXTENSIONS = {
    ".cs",
    ".razor",
    ".cshtml",
    ".css",
    ".js",
    ".json",
    ".xml",
    ".csproj",
    ".sln",
    ".props",
    ".targets",
    ".dart",
    ".yaml",
    ".yml",
}

SKIP_DIRS = {
    ".git",
    ".vs",
    ".idea",
    ".dart_tool",
    ".gradle",
    ".build-api-output",
    "bin",
    "obj",
    "build",
    "Debug",
    "Release",
    "node_modules",
    "packages",
    ".pub-cache",
    "ios/Pods",
    "android/.gradle",
}

SKIP_FILES = {
    "pubspec.lock",
    "package-lock.json",
    "yarn.lock",
}


def should_skip_dir(path: Path) -> bool:
    normalized = path.as_posix()
    return path.name in SKIP_DIRS or any(part in SKIP_DIRS for part in path.parts) or any(
        normalized.endswith(skip) for skip in SKIP_DIRS
    )


def iter_code_files(root: Path) -> list[Path]:
    files: list[Path] = []
    for current_root, dir_names, file_names in os.walk(root):
        current_path = Path(current_root)
        dir_names[:] = [
            name for name in dir_names if not should_skip_dir(current_path / name)
        ]

        for file_name in file_names:
            file_path = current_path / file_name
            if file_name in SKIP_FILES:
                continue
            if file_path.suffix.lower() not in CODE_EXTENSIONS:
                continue
            if file_path.stat().st_size > 2_500_000:
                continue
            files.append(file_path)

    return sorted(files, key=lambda p: p.relative_to(root).as_posix().lower())


def read_text(path: Path) -> str:
    for encoding in ("utf-8-sig", "utf-8", "cp1251"):
        try:
            return path.read_text(encoding=encoding)
        except UnicodeDecodeError:
            continue
    return path.read_text(encoding="utf-8", errors="replace")


def paragraph(text: str, style: str = "Normal") -> str:
    text = html.escape(text)
    style_xml = ""
    if style:
        style_xml = f'<w:pPr><w:pStyle w:val="{style}"/></w:pPr>'
    return f'<w:p>{style_xml}<w:r><w:t xml:space="preserve">{text}</w:t></w:r></w:p>'


def file_name_paragraph(text: str) -> str:
    text = html.escape(text)
    return (
        '<w:p><w:pPr><w:pStyle w:val="FileName"/></w:pPr>'
        f'<w:r><w:t xml:space="preserve">{text}</w:t></w:r></w:p>'
    )


def code_paragraph(line: str) -> str:
    line = html.escape(line)
    return (
        '<w:p><w:pPr><w:pStyle w:val="Code"/></w:pPr>'
        f'<w:r><w:t xml:space="preserve">{line}</w:t></w:r></w:p>'
    )


def page_break() -> str:
    return '<w:p><w:r><w:br w:type="page"/></w:r></w:p>'


def make_document_xml(sections: list[tuple[str, Path, list[Path]]]) -> str:
    body: list[str] = [paragraph("Исходный код StepTreck", "Title")]
    number = 1

    for title, root, files in sections:
        body.append(paragraph(title, "Heading1"))

        for file_path in files:
            body.append(file_name_paragraph(f"{number}) {file_path.name}"))

            text = read_text(file_path)
            if not text.strip():
                body.append(code_paragraph("[пустой файл]"))
            else:
                for line in text.splitlines():
                    body.append(code_paragraph(line))

            number += 1

    return f'''<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
  <w:body>
    {''.join(body)}
    <w:sectPr>
      <w:pgSz w:w="11906" w:h="16838"/>
      <w:pgMar w:top="850" w:right="850" w:bottom="850" w:left="850" w:header="708" w:footer="708" w:gutter="0"/>
    </w:sectPr>
  </w:body>
</w:document>
'''


def make_styles_xml() -> str:
    return '''<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<w:styles xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
  <w:style w:type="paragraph" w:default="1" w:styleId="Normal">
    <w:name w:val="Normal"/>
    <w:rPr>
      <w:rFonts w:ascii="Times New Roman" w:hAnsi="Times New Roman" w:cs="Times New Roman"/>
      <w:sz w:val="24"/>
    </w:rPr>
  </w:style>
  <w:style w:type="paragraph" w:styleId="Title">
    <w:name w:val="Title"/>
    <w:basedOn w:val="Normal"/>
    <w:pPr><w:jc w:val="center"/><w:spacing w:after="240"/></w:pPr>
    <w:rPr><w:b/><w:sz w:val="32"/></w:rPr>
  </w:style>
  <w:style w:type="paragraph" w:styleId="Heading1">
    <w:name w:val="heading 1"/>
    <w:basedOn w:val="Normal"/>
    <w:pPr><w:spacing w:before="360" w:after="160"/></w:pPr>
    <w:rPr><w:b/><w:sz w:val="30"/></w:rPr>
  </w:style>
  <w:style w:type="paragraph" w:styleId="Heading2">
    <w:name w:val="heading 2"/>
    <w:basedOn w:val="Normal"/>
    <w:pPr><w:spacing w:before="240" w:after="80"/></w:pPr>
    <w:rPr><w:b/><w:sz w:val="26"/></w:rPr>
  </w:style>
  <w:style w:type="paragraph" w:styleId="FileName">
    <w:name w:val="FileName"/>
    <w:basedOn w:val="Normal"/>
    <w:pPr><w:spacing w:before="240" w:after="80"/></w:pPr>
    <w:rPr><w:b/><w:sz w:val="24"/></w:rPr>
  </w:style>
  <w:style w:type="paragraph" w:styleId="Code">
    <w:name w:val="Code"/>
    <w:basedOn w:val="Normal"/>
    <w:pPr><w:spacing w:before="0" w:after="0" w:line="240" w:lineRule="auto"/></w:pPr>
    <w:rPr>
      <w:rFonts w:ascii="Consolas" w:hAnsi="Consolas" w:cs="Consolas"/>
      <w:sz w:val="16"/>
    </w:rPr>
  </w:style>
</w:styles>
'''


def write_docx(output: Path, document_xml: str) -> None:
    output.parent.mkdir(parents=True, exist_ok=True)
    if output.exists():
        output.unlink()

    content_types = '''<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
  <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
  <Default Extension="xml" ContentType="application/xml"/>
  <Override PartName="/word/document.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml"/>
  <Override PartName="/word/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.styles+xml"/>
</Types>
'''
    package_rels = '''<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="word/document.xml"/>
</Relationships>
'''
    document_rels = '''<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/>
</Relationships>
'''

    with zipfile.ZipFile(output, "w", zipfile.ZIP_DEFLATED) as docx:
        docx.writestr("[Content_Types].xml", content_types)
        docx.writestr("_rels/.rels", package_rels)
        docx.writestr("word/document.xml", document_xml)
        docx.writestr("word/styles.xml", make_styles_xml())
        docx.writestr("word/_rels/document.xml.rels", document_rels)


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--web-root", default=r"C:\Users\meloc\source\repos\steptreck")
    parser.add_argument("--domain-root", default=r"C:\Users\meloc\source\repos\steptreck\steptrek.Domain")
    parser.add_argument("--mobile-root", default=r"C:\Users\meloc\StudioProjects\steptreck_mobile")
    parser.add_argument("--output", default=r"C:\Users\meloc\Desktop\Diplom\Исходный код StepTreck Web Domain и Mobile.docx")
    args = parser.parse_args()

    repo_root = Path(args.web_root)
    api_root = repo_root / "steptreck"
    web_root = repo_root / "steptreck.Web"
    domain_root = Path(args.domain_root)
    mobile_root = Path(args.mobile_root)
    output = Path(args.output)

    sections = [
        ("API", api_root, iter_code_files(api_root)),
        ("WEB", web_root, iter_code_files(web_root)),
        ("Domain", domain_root, iter_code_files(domain_root)),
        ("Мобильное приложение", mobile_root, iter_code_files(mobile_root)),
    ]

    document_xml = make_document_xml(sections)
    write_docx(output, document_xml)

    total_files = sum(len(files) for _, _, files in sections)
    print(f"Готово: {output}")
    print(f"Файлов добавлено: {total_files}")


if __name__ == "__main__":
    main()
