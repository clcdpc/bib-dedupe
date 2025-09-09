class BrowserDecisionStore {
  constructor(key = 'decisionCart') {
    this.key = key;
  }
  _load() {
    return JSON.parse(localStorage.getItem(this.key) || '[]');
  }
  _save(items) {
    localStorage.setItem(this.key, JSON.stringify(items));
  }
  add(decision) {
    const items = this._load();
    const idx = items.findIndex(d => d.leftBibId === decision.leftBibId && d.rightBibId === decision.rightBibId);
    if (idx >= 0) items[idx] = decision; else items.push(decision);
    this._save(items);
  }
  getAll() { return this._load(); }
  remove(leftBibId, rightBibId) {
    const items = this._load().filter(d => !(d.leftBibId === leftBibId && d.rightBibId === rightBibId));
    this._save(items);
  }
  count() { return this._load().length; }
}
export { BrowserDecisionStore };
