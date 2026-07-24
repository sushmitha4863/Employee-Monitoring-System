import "./StatCard.css";

const StatCard = ({
    title,
    value,
    subtitle,
    icon,
    color
}) => {

    return (

        <div className="stat-card">

            <div className="stat-content">

                <div>

                    <p className="stat-title">

                        {title}

                    </p>

                    <h2>

                        {value}

                    </h2>

                    <span>

                        {subtitle}

                    </span>

                </div>

                <div
                    className="stat-icon"
                    style={{
                        background: `${color}15`,
                        color
                    }}
                >

                    {icon}

                </div>

            </div>

        </div>

    );

};

export default StatCard;