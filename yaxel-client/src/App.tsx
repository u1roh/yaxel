import React from 'react';
import logo from './logo.svg';
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
    fetch('function')
      .then(res => res?.text())
      .then(body => {
        console.log(body);
        this.setState({ functions: JSON.parse(body) })
      });
  }
  render() {
    return (
      <div className="FuncList">
        <h1>Functions</h1>
        <ul>{this.state.functions.map(item => <li>{item}</li>)}</ul>
      </div>
    );
  }
}

function App() {
  // <Function name="hoge" />
  return (
    <div className="App">
      <FuncList />
      <Function name="simple" />
      <Function name="piyo" />
      <Function name="hoge" />
    </div>
  );
}

export default App;
