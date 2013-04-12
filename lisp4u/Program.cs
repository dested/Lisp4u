using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace lisp4u
{
    class Program
    {
        static void Main(string[] args)
        {
            ParseLisp p = new ParseLisp(

                @"
(defun abba (a b) (if (< (* a b) (90000000000)) (abba (* a b) (* a b 4)) (* a b)))
(defun babba (a b) (if (< (* a b) (90000000000)) (babba (* a b 6) (* a b)) (* a b)))

(* 48 (+ 5 7 (abba 5 10) (babba 5 10)) (+ 4 6 (abba 45 10) (babba 5 480)) (* 48 (+ 5 7 (abba 5 10) (babba 5 10)) (+ 4 6 (abba 45 10) (babba 5 480)) (* 48 (+ 5 7 (abba 5 10) (babba 5 10)) (+ 4 6 (abba 45 10) (babba 5 480))) ) )"


                );
            p.run(false); 
            p = new ParseLisp(

                @"(defun dohanoi (n to from u)
  (cond
    (
      (> n 0)
      (dohanoi (- n 1) u from to) 
        
 
      (dohanoi (- n 1) to u from)
    )
  )
)

(defun hanoi (n)
    write-line(""C"")
    (dohanoi n ""C"" ""A"" ""B"")
)

 (hanoi 10 )
"

                );
            p.run(false);


            p = new ParseLisp(@"(defun fib (n)
  (   
    (if (< n 2)
    (n)
    (+ (fib (- n 1)) (fib (- n 2))))))


(fib 50)


");
            p.run(false);
        }
    }
}
