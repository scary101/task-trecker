from __future__ import annotations

import argparse
import shutil
import tempfile
import zipfile
from pathlib import Path
from xml.etree import ElementTree as ET


NS = {"w": "http://schemas.openxmlformats.org/wordprocessingml/2006/main"}
W = f"{{{NS['w']}}}"


def qn(name: str) -> str:
    return W + name


def child(parent: ET.Element, name: str) -> ET.Element:
    found = parent.find(f"w:{name}", NS)
    if found is None:
        found = ET.SubElement(parent, qn(name))
    return found


def set_attr(element: ET.Element, name: str, value: str) -> None:
    element.set(qn(name), value)


def ensure_rpr(parent: ET.Element) -> ET.Element:
    return child(parent, "rPr")


def set_run_font(rpr: ET.Element, size: str) -> None:
    fonts = child(rpr, "rFonts")
    for attr in ("ascii", "hAnsi", "cs", "eastAsia"):
        set_attr(fonts, attr, "Times New Roman")
    set_attr(child(rpr, "sz"), "val", size)
    set_attr(child(rpr, "szCs"), "val", size)


def set_normal_style(styles_root: ET.Element) -> None:
    normal = None
    for style in styles_root.findall("w:style", NS):
        if style.get(qn("styleId")) == "Normal":
            normal = style
            break
    if normal is None:
        normal = ET.SubElement(styles_root, qn("style"))
        set_attr(normal, "type", "paragraph")
        set_attr(normal, "default", "1")
        set_attr(normal, "styleId", "Normal")

    ppr = child(normal, "pPr")
    set_attr(child(ppr, "jc"), "val", "both")
    spacing = child(ppr, "spacing")
    set_attr(spacing, "line", "360")
    set_attr(spacing, "lineRule", "auto")
    set_attr(spacing, "before", "0")
    set_attr(spacing, "after", "0")
    ind = child(ppr, "ind")
    set_attr(ind, "firstLine", "708")

    set_run_font(ensure_rpr(normal), "28")


def format_sections(document_root: ET.Element) -> None:
    for sect in document_root.findall(".//w:sectPr", NS):
        pg_size = child(sect, "pgSz")
        set_attr(pg_size, "w", "11906")
        set_attr(pg_size, "h", "16838")
        pg_size.attrib.pop(qn("orient"), None)

        margins = child(sect, "pgMar")
        set_attr(margins, "top", "1134")
        set_attr(margins, "bottom", "1134")
        set_attr(margins, "left", "1701")
        set_attr(margins, "right", "567")
        set_attr(margins, "header", "708")
        set_attr(margins, "footer", "708")
        set_attr(margins, "gutter", "0")


def format_paragraphs(document_root: ET.Element) -> None:
    for paragraph in document_root.findall(".//w:p", NS):
        ppr = child(paragraph, "pPr")
        set_attr(child(ppr, "jc"), "val", "both")
        spacing = child(ppr, "spacing")
        set_attr(spacing, "line", "360")
        set_attr(spacing, "lineRule", "auto")
        set_attr(spacing, "before", "0")
        set_attr(spacing, "after", "0")

        if not any(parent_tag.endswith("tc") for parent_tag in []):
            ind = child(ppr, "ind")
            set_attr(ind, "firstLine", "708")

        for run in paragraph.findall("w:r", NS):
            set_run_font(ensure_rpr(run), "28")


def format_tables(document_root: ET.Element) -> None:
    for table in document_root.findall(".//w:tbl", NS):
        tbl_pr = child(table, "tblPr")
        tbl_w = child(tbl_pr, "tblW")
        set_attr(tbl_w, "type", "pct")
        set_attr(tbl_w, "w", "5000")
        layout = child(tbl_pr, "tblLayout")
        set_attr(layout, "type", "autofit")

        borders = child(tbl_pr, "tblBorders")
        for border_name in ("top", "left", "bottom", "right", "insideH", "insideV"):
            border = child(borders, border_name)
            set_attr(border, "val", "single")
            set_attr(border, "sz", "4")
            set_attr(border, "space", "0")
            set_attr(border, "color", "000000")

        for paragraph in table.findall(".//w:p", NS):
            ppr = child(paragraph, "pPr")
            set_attr(child(ppr, "jc"), "val", "center")
            spacing = child(ppr, "spacing")
            set_attr(spacing, "line", "240")
            set_attr(spacing, "lineRule", "auto")
            set_attr(spacing, "before", "0")
            set_attr(spacing, "after", "0")
            ind = child(ppr, "ind")
            set_attr(ind, "firstLine", "0")

            for run in paragraph.findall("w:r", NS):
                set_run_font(ensure_rpr(run), "24")

        for cell in table.findall(".//w:tc", NS):
            tc_pr = child(cell, "tcPr")
            margins = child(tc_pr, "tcMar")
            for margin_name in ("top", "bottom"):
                margin = child(margins, margin_name)
                set_attr(margin, "w", "57")
                set_attr(margin, "type", "dxa")
            for margin_name in ("left", "right"):
                margin = child(margins, margin_name)
                set_attr(margin, "w", "85")
                set_attr(margin, "type", "dxa")


def format_docx(input_path: Path, output_path: Path) -> None:
    with tempfile.TemporaryDirectory() as tmp:
        tmp_path = Path(tmp)
        with zipfile.ZipFile(input_path, "r") as archive:
            archive.extractall(tmp_path)

        document_xml = tmp_path / "word" / "document.xml"
        styles_xml = tmp_path / "word" / "styles.xml"

        ET.register_namespace("w", NS["w"])

        document_tree = ET.parse(document_xml)
        document_root = document_tree.getroot()
        format_sections(document_root)
        format_paragraphs(document_root)
        format_tables(document_root)
        document_tree.write(document_xml, encoding="utf-8", xml_declaration=True)

        if styles_xml.exists():
            styles_tree = ET.parse(styles_xml)
            set_normal_style(styles_tree.getroot())
            styles_tree.write(styles_xml, encoding="utf-8", xml_declaration=True)

        if output_path.exists():
            output_path.unlink()
        with zipfile.ZipFile(output_path, "w", zipfile.ZIP_DEFLATED) as archive:
            for file in tmp_path.rglob("*"):
                if file.is_file():
                    archive.write(file, file.relative_to(tmp_path).as_posix())


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("docx", nargs="?", default=r"C:\Users\meloc\Desktop\Diplom\Таблицы_модулей_проектов_StepTreck.docx")
    parser.add_argument("--output")
    args = parser.parse_args()

    docx_path = Path(args.docx)
    output_path = Path(args.output) if args.output else docx_path
    backup_path = docx_path.with_name(docx_path.stem + "_до_ГОСТ" + docx_path.suffix)

    if output_path == docx_path and not backup_path.exists():
        shutil.copy2(docx_path, backup_path)

    format_docx(docx_path, output_path)
    print(f"Готово: {output_path}")
    if output_path == docx_path:
        print(f"Резервная копия: {backup_path}")


if __name__ == "__main__":
    main()
