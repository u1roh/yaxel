import React from 'react';
import './App.css';
import Function from './Function'

interface State {
  functions: string[]
}

class FuncList extends React.Component<{}, State> {
  private breathCount: number = 0;
  constructor(props: {}) {
    super(props);
    this.state = { functions: [] };
  }
  private async fetchFunctions() {
    const res = await fetch('api/function');
    const txt = await res.text();
    this.setState({ functions: JSON.parse(txt) });
  }
  componentDidMount() {
    setInterval(async () => {
      const res = await fetch('api/breath/');
      const txt = await res.text();
      const breath = Number.parseInt(txt);
      if (breath != this.breathCount) {
        console.log("breath = " + breath);
        this.breathCount = breath;
        this.fetchFunctions();
      }
    }, 1000);
    this.fetchFunctions();
  }
  render() {
    return (
      <div className="FuncList">
        <h1>Functions</h1>
        {this.state.functions.map(item => <Function name={item}></Function>)}
      </div>
    );
  }
}

class CodeEditor extends React.Component<{}, { code: string }> {
  constructor(props: {}) {
    super(props);
    this.state = { code: "" };
  }
  componentDidMount() {
    fetch("api/usercode")
      .then(response => response.text())
      .then(text => this.setState({ code: text }));
  }
  private onKeyDown(e: React.KeyboardEvent<HTMLTextAreaElement>) {
    if (e.ctrlKey && e.key == 's') {
      e.preventDefault();
      console.log("Ctrl + S");
      fetch('api/update-usercode', {
        method: 'POST',
        body: this.state.code,
        headers: {
          'Content-Type': 'application/json'
        }
      });
    }
  }
  render() {
    return <textarea
      className="CodeEditor"
      value={this.state.code}
      onChange={e => this.setState({ code: e.target.value })}
      onKeyDown={e => this.onKeyDown(e)}></textarea>
  }
}

function App() {
  return (
    <div className="App">
      <div className="left"><FuncList /></div>
      <div className="right"><CodeEditor /></div>
    </div>
  );
}

export default App;
