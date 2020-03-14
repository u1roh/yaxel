import React from 'react';

interface Props {
    name: string
}

interface State {
    text: string
}

class Function extends React.Component<Props, State> {
    constructor(props: Props) {
        super(props);
        this.state = { text: "hoge" };
    }
    componentDidMount() {
        fetch('function/' + this.props.name)
            .then(response => response?.text())
            .then(t => this.setState({ text: t }));
    }
    render() {
        return (
            <div className="Function">
                <div className="Function-name">{this.props.name}</div>
                <p>{this.state.text}</p>
            </div>
        );
    }
}

export default Function;
