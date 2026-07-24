import "./EmployeePagination.css";

const EmployeePagination = () => {
    return (
        <div className="employee-pagination">

            <div className="pagination-info">
                Showing 1 - 4 of 98 Employees
            </div>

            <div className="pagination-buttons">
                <button>Previous</button>
                <button className="active-page">1</button>
                <button>2</button>
                <button>3</button>
                <button>...</button>
                <button>25</button>
                <button>Next</button>
            </div>

        </div>
    );
};

export default EmployeePagination;
