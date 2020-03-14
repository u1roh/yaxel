import React from 'react';
import logo from './logo.svg';
import './App.css';

interface State {
  text: string
}

class App extends React.Component<{}, State> {
  constructor(props: {}) {
    super(props);
    this.state = { text: "hoge" };
  }
  componentDidMount() {
    fetch('function/hoge')
      .then(response => response?.text())
      .then(t => this.setState({ text: t }));
  }
  render() {
    return (
      <div className="App">
        <header className="App-header">
          <img src={logo} className="App-logo" alt="logo" />
          <p>
            Edit <code>src/App.tsx</code> and save to reload.
        </p>
          <p>aaa bbb</p>
          <p>{this.state.text}</p>
          <a
            className="App-link"
            href="https://reactjs.org"
            target="_blank"
            rel="noopener noreferrer"
          >
            Learn React
        </a>
        </header>
      </div>
    );

  }
}

export default App;
