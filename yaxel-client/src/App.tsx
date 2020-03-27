import React from 'react';
import './App.css';
import Function from './Function'

interface State {
  functions: string[]
}

class FuncList extends React.Component<{}, State> {
  constructor(props: {}) {
    super(props);
    this.state = { functions: [] };
  }
  componentDidMount() {
    fetch('api/function')
      .then(res => res?.text())
      .then(body => this.setState({ functions: JSON.parse(body) }));
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
  render() {
    return <textarea className="CodeEditor" value={this.state.code}></textarea>
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
