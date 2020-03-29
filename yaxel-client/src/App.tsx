import React from 'react';
import './App.css';
import Function from './Function'
import * as api from './api'

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
    const functions = await api.fetchFunctionList();
    console.log("FuncList: functions = " + JSON.stringify(functions));
    this.setState({ functions: functions });
  }
  componentDidMount() {
    setInterval(async () => {
      const breath = await api.fetchBreathCount();
      if (breath !== this.breathCount) {
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
    api.fetchUserCode()
      .then(text => this.setState({ code: text }));
  }
  private onKeyDown(e: React.KeyboardEvent<HTMLTextAreaElement>) {
    if (e.ctrlKey && e.key === 's') {
      e.preventDefault();
      console.log("Ctrl + S");
      api.updateUserCode(this.state.code);
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
