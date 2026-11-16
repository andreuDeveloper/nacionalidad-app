#!/usr/bin/env python3
"""
Genera el banco de preguntas del CCSE en JSON a partir del manual oficial en PDF.

Uso (desde la raíz del repositorio):
    pip install pymupdf
    python3 tools/ccse/build_ccse_bank.py docs/CCSE_2026.pdf src/Nacionalidad.App/Content/ccse.json --content-version 2026.1

Qué hace:
  1. Lee las preguntas de las páginas "PREGUNTAS" de cada tarea (maquetadas a dos columnas)
     usando las coordenadas de cada palabra.
  2. Lee las soluciones de las últimas páginas del manual.
  3. Añade las explicaciones de tools/ccse/explicaciones_<idioma>.tsv (id<TAB>texto).
  4. Escribe el JSON con schemaVersion 1 que entiende ExamEngine.Core.

Cuando el Cervantes publique el manual del año siguiente: pasar el PDF nuevo, revisar el
informe de preguntas nuevas/eliminadas que imprime el script y completar las explicaciones
que falten en los .tsv.
"""
import argparse, glob, json, os, re, sys

try:
    import pymupdf
except ImportError:  # versiones antiguas
    import fitz as pymupdf

HERE = os.path.dirname(os.path.abspath(__file__))
ID = re.compile(r'^[1-5]\d{3}$')

CATEGORIES = [
    ("tarea1", "Gobierno, legislación y participación ciudadana"),
    ("tarea2", "Derechos y deberes fundamentales"),
    ("tarea3", "Organización territorial de España. Geografía física y política"),
    ("tarea4", "Cultura e historia de España"),
    ("tarea5", "Sociedad española"),
]
# Formato oficial de la prueba: 25 preguntas, 45 minutos, aprobado con 15 aciertos.
EXAM_SECTIONS = [("tarea1", 10), ("tarea2", 3), ("tarea3", 2), ("tarea4", 5), ("tarea5", 5)]


def is_question_id(text):
    return ID.match(text) and 1 <= int(text) % 1000 <= 200


def find_question_pages(doc):
    """Páginas con preguntas: tienen varios ids de pregunta al principio de línea y no son de soluciones."""
    pages = []
    end = solution_start(doc)
    for i, page in enumerate(doc):
        if i >= end:
            break
        text = page.get_text()
        ids = [w for w in page.get_text('words') if is_question_id(w[4])]
        heads = [w for w in ids if re.search(r'(?m)^\s*' + w[4] + r'\b', text)]
        if len(heads) >= 4:
            pages.append(i)
    return pages


def lines_in_region(words):
    words = sorted(words, key=lambda w: (round(w[3]), w[0]))
    lines, cur, ly = [], [], None
    for w in words:
        if ly is None or abs(w[3] - ly) > 3:
            if cur:
                lines.append(' '.join(x[4] for x in cur))
            cur, ly = [w], w[3]
        else:
            cur.append(w)
    if cur:
        lines.append(' '.join(x[4] for x in cur))
    return lines


def extract_questions(doc):
    raw = {}
    for pno in find_question_pages(doc):
        page = doc[pno]
        W, H = page.rect.width, page.rect.height
        words = [w for w in page.get_text('words') if 60 < w[1] < H - 50]  # sin cabecera ni pie
        ids = [w for w in words if is_question_id(w[4])]
        # El id tiene que ser lo primero de su línea dentro de su columna.
        ids = [w for w in ids if not any(abs(o[1] - w[1]) < 3 and w[0] - 12 < o[2] < w[0] + 1 and o[0] < w[0] for o in words)]
        if not ids:
            continue
        xs = sorted(set(round(w[0]) for w in ids))
        starts = []
        for x in xs:
            if not starts or x - starts[-1] > 60:
                starts.append(x)
        sidebar = len(starts) == 1 and starts[0] > 200  # página con columna lateral de texto a la izquierda
        colsplit = (starts[1] - 5) if len(starts) > 1 else (starts[0] - 5 if sidebar else W / 2)
        for idw in ids:
            left = idw[0] < colsplit
            xl, xr = (0, colsplit) if left else (colsplit, W)
            if sidebar:
                xl, xr = colsplit, W
            nexts = [o[1] for o in ids if o is not idw and (o[0] < colsplit) == left and o[1] > idw[1] + 2]
            yb = min(nexts) if nexts else H
            region = [w for w in words if xl <= w[0] < xr and idw[1] - 3 <= w[1] < yb - 3 and w is not idw]
            raw[idw[4]] = {'page': pno + 1, 'lines': lines_in_region(region)}
    return raw


def solution_start(doc):
    """Las soluciones van desde la página con el título SOLUCIONES hasta el final del manual."""
    for i, page in enumerate(doc):
        if 'SOLUCIONES' in page.get_text():
            return i
    return len(doc)


def extract_solutions(doc):
    sol = {}
    for page in list(doc)[solution_start(doc):]:
        ws = page.get_text('words')
        for w in ws:
            if is_question_id(w[4]):
                cand = [o for o in ws if abs(o[1] - w[1]) < 4 and 0 < o[0] - w[2] < 60 and re.match(r'^[abc]$', o[4])]
                if cand:
                    sol[w[4]] = min(cand, key=lambda o: o[0])[4]
    return sol


def clean(s):
    s = re.sub(r'\s+', ' ', s).strip()
    s = re.sub(r'(\w)- (\w)', r'\1\2', s)  # palabras partidas a final de línea
    s = s.replace('….', '…').replace('....', '...')
    return s


def parse(lines):
    text, opts = [], []
    for l in lines:
        m = re.match(r'^([abc])\.\s*(.*)$', l)
        if m and len(opts) == ord(m.group(1)) - 97:
            opts.append(m.group(2))
            continue
        if opts:
            opts[-1] += ' ' + l
        else:
            text.append(l)
    opts = [clean(o) for o in opts]
    # Puntuación homogénea: si alguna opción acaba en punto, todas.
    if any(o.endswith('.') for o in opts):
        opts = [o if o.endswith(('.', '…', '?', '!')) else o + '.' for o in opts]
    return clean(' '.join(text)), opts


def load_explanations():
    result = {}
    for path in sorted(glob.glob(os.path.join(HERE, 'explicaciones_*.tsv'))):
        lang = os.path.basename(path)[len('explicaciones_'):-len('.tsv')]
        with open(path, encoding='utf-8') as f:
            for n, line in enumerate(f, 1):
                line = line.rstrip('\n')
                if not line.strip() or line.startswith('#'):
                    continue
                if '\t' not in line:
                    sys.exit(f'{path}:{n}: falta el tabulador entre id y texto')
                qid, text = line.split('\t', 1)
                result.setdefault(qid.strip(), {})[lang] = text.strip()
    return result


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('pdf')
    ap.add_argument('out')
    ap.add_argument('--content-version', default='2026.1')
    ap.add_argument('--year', default='2026')
    args = ap.parse_args()

    doc = pymupdf.open(args.pdf)
    raw = extract_questions(doc)
    sol = extract_solutions(doc)
    expl = load_explanations()

    problems, questions = [], []
    for qid in sorted(raw, key=int):
        prompt, opts = parse(raw[qid]['lines'])
        if qid not in sol:
            problems.append(f'{qid}: sin solución')
            continue
        if len(opts) not in (2, 3) or not prompt:
            problems.append(f'{qid}: no se pudo leer bien ({raw[qid]["lines"]})')
            continue
        is_tf = len(opts) == 2 and opts[0].rstrip('.').lower() == 'verdadero'
        q = {
            'id': f'ccse-{qid}',
            'categoryId': f'tarea{qid[0]}',
            'prompt': {'es': prompt},
            'options': [{'id': 'abc'[i], 'text': {'es': o}} for i, o in enumerate(opts)],
            'correctOptionId': sol[qid],
            'shuffleOptions': not is_tf,
            'reference': f'Manual CCSE {args.year}, p. {raw[qid]["page"]}, pregunta {qid}',
        }
        if qid in expl:
            q['explanation'] = expl[qid]
        else:
            problems.append(f'{qid}: sin explicación en español')
        if is_tf:
            q['tags'] = ['verdadero-falso']
        questions.append(q)

    bank = {
        'schemaVersion': 1,
        'id': 'ccse',
        'contentVersion': args.content_version,
        'defaultLanguage': 'es',
        'title': {'es': f'CCSE {args.year}'},
        'source': f'Manual de preparación de la prueba CCSE {args.year}, Instituto Cervantes. Explicaciones: elaboración propia.',
        'categories': [{'id': cid, 'title': {'es': title}, 'order': i + 1} for i, (cid, title) in enumerate(CATEGORIES)],
        'exams': [{
            'id': 'ccse-oficial',
            'title': {'es': 'Simulacro CCSE'},
            'timeLimitMinutes': 45,
            'passMark': 15,
            'sections': [{'categoryId': c, 'questionCount': n} for c, n in EXAM_SECTIONS],
        }],
        'questions': questions,
    }

    # Informe de cambios respecto al banco anterior, si existe.
    if os.path.exists(args.out):
        with open(args.out, encoding='utf-8') as f:
            old = {q['id']: q for q in json.load(f)['questions']}
        new = {q['id']: q for q in questions}
        added = sorted(set(new) - set(old))
        removed = sorted(set(old) - set(new))
        changed = sorted(k for k in set(new) & set(old) if new[k]['prompt'] != old[k]['prompt'] or new[k]['options'] != old[k]['options'])
        print(f'Nuevas: {len(added)}  Eliminadas: {len(removed)}  Cambiadas: {len(changed)}')
        for label, ids in (('Nuevas', added), ('Eliminadas', removed), ('Cambiadas', changed)):
            if ids:
                print(f'  {label}: {", ".join(ids)}')

    with open(args.out, 'w', encoding='utf-8') as f:
        json.dump(bank, f, ensure_ascii=False, indent=2)
        f.write('\n')

    by_cat = {}
    for q in questions:
        by_cat[q['categoryId']] = by_cat.get(q['categoryId'], 0) + 1
    print(f'{len(questions)} preguntas -> {args.out}  {by_cat}')
    if problems:
        print('AVISOS:')
        for p in problems:
            print('  ' + p)
        sys.exit(1)


if __name__ == '__main__':
    main()
