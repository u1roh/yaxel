import React from 'react';
import * as yaxel from './yaxel'
import './TypedInput.css';

class TypedInput extends React.Component<yaxel.TypedItem> {
    constructor(props: yaxel.TypedItem) {
        super(props);
        this.state = {};
    }
    private renderInternal() {
        switch (this.props.type) {
            case "int":
            case "float":
            case "bool":
            case "string":
                return <span>{this.props.name} = <input></input></span>;
            default:
                if (Array.isArray(this.props.type)) {
                    return JSON.stringify(this.props.type);
                }
                switch (this.props.type.tag) {
                    case 'record':
                        return (<div>
                            <div>{this.props.name} = {this.props.type.name}</div>
                            {this.props.type.fields.map(item =>
                                <div><TypedInput name={item.name} type={item.type} /></div>
                            )}
                        </div>);
                    case 'union':
                        return (<div>
                            {this.props.name} =
                            <select>
                                {this.props.type.cases.map(item =>
                                    <option value={item.name}>{item.name}</option>)}
                            </select>
                        </div>);
                    default:
                        return JSON.stringify(this.props.type);
                }
        }
    }
    render() {
        return <div className="TypedInput">{this.renderInternal()}</div>;
    }
}

export default TypedInput;
