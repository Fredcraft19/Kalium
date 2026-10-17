const vscode = require('vscode');

const TYPES = [
  'uint8', 'uint16', 'uint32', 'uint64',
  'int8', 'int16', 'int32', 'int64',
  'float32', 'float64',
  'bool', 'uint', 'int', 'float', 'void'
];

const TYPE_PATTERN = TYPES.join('|');

const DECLARATION = new RegExp(`\\b(${TYPE_PATTERN})\\s+([A-Za-z_]\\w*)`, 'g');

const NAMING_NEW_VARIABLE = new RegExp(`\\b(${TYPE_PATTERN})\\s+\\w*$`);

function activate(context) {
  const provider = vscode.languages.registerCompletionItemProvider('wisp', {
    provideCompletionItems(document, position) {
      const linePrefix = document.lineAt(position).text.slice(0, position.character);

      if (NAMING_NEW_VARIABLE.test(linePrefix)) {
        return [];
      }

      const items = [];

      for (const type of TYPES) {
        const item = new vscode.CompletionItem(type, vscode.CompletionItemKind.TypeParameter);
        item.detail = 'data type';
        items.push(item);
      }

      const seen = new Set();
      const text = document.getText();
      for (const match of text.matchAll(DECLARATION)) {
        const [, type, name] = match;
        if (seen.has(name)) continue;
        seen.add(name);
        const item = new vscode.CompletionItem(name, vscode.CompletionItemKind.Variable);
        item.detail = type;
        items.push(item);
      }

      return items;
    }
  });

  context.subscriptions.push(provider);
}

function deactivate() {}

module.exports = { activate, deactivate };