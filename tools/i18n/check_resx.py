#!/usr/bin/env python3
"""
Comprueba que todos los AppResources.<idioma>.resx tienen las mismas claves que el neutro (español)
y los mismos marcadores {0}, {1}... Sale con código 1 si hay diferencias.

Uso: python3 tools/i18n/check_resx.py src/Nacionalidad.App/Resources/Strings
"""
import glob, os, re, sys
import xml.etree.ElementTree as ET


def load(path):
    root = ET.parse(path).getroot()
    return {d.get('name'): (d.findtext('value') or '') for d in root.findall('data')}


def placeholders(s):
    return sorted(set(re.findall(r'\{\d+\}', s)))


def main(folder):
    neutral_path = os.path.join(folder, 'AppResources.resx')
    neutral = load(neutral_path)
    problems = []
    for path in sorted(glob.glob(os.path.join(folder, 'AppResources.*.resx'))):
        lang = os.path.basename(path).split('.')[1]
        other = load(path)
        for k in sorted(set(neutral) - set(other)):
            problems.append(f'[{lang}] falta la clave {k}')
        for k in sorted(set(other) - set(neutral)):
            problems.append(f'[{lang}] clave que no existe en español: {k}')
        for k in sorted(set(neutral) & set(other)):
            if not other[k].strip():
                problems.append(f'[{lang}] {k} está vacía')
            elif placeholders(neutral[k]) != placeholders(other[k]):
                problems.append(f'[{lang}] {k}: marcadores {placeholders(other[k])} y en español {placeholders(neutral[k])}')
    print(f'{len(neutral)} claves en español.')
    for p in problems:
        print(p)
    return 1 if problems else 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1] if len(sys.argv) > 1 else 'src/Nacionalidad.App/Resources/Strings'))
